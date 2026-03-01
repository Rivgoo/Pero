using System.Text;
using System.Text.RegularExpressions;
using Pero.Abstractions.Contracts;
using Pero.Abstractions.Models;
using Pero.Kernel.Dictionaries;
using Pero.Kernel.Rules;

namespace Pero.Languages.Uk_UA.Rules.Typography;

public class NumberAndUnitAnalyzer : BaseAnalyzer
{
	public override string Name => "NumbersAndUnits";

	#region Rule IDs
	private const string MissingSpaceUnitId = "UK_UA_NUM_MISSING_SPACE_UNIT";
	private const string MissingSpacePercentId = "UK_UA_NUM_MISSING_SPACE_PERCENT";
	private const string MissingHyphenEndingId = "UK_UA_NUM_MISSING_HYPHEN_ENDING";
	private const string WrongEndingId = "UK_UA_NUM_WRONG_ENDING";
	private const string SpacedRangeId = "UK_UA_NUM_SPACED_RANGE";
	private const string WrongDashRangeId = "UK_UA_NUM_WRONG_DASH_RANGE";
	private const string RomanCyrillicId = "UK_UA_NUM_ROMAN_CYRILLIC";
	#endregion

	private readonly FstSuffixDictionary<UkMorphologyTag> _dictionary;

	private static readonly Regex RomanNumeralRegex = new(
		@"^M{0,4}(CM|CD|D?C{0,3})(XC|XL|L?X{0,3})(IX|IV|V?I{0,3})$",
		RegexOptions.Compiled | RegexOptions.IgnoreCase);

	private static readonly Dictionary<char, char> CyrillicToLatinRoman = new()
	{
		{'І', 'I'}, {'Х', 'X'}, {'С', 'C'}, {'М', 'M'}, {'В', 'V'}, {'Л', 'L'}, {'Д', 'D'}
	};

	private static readonly HashSet<char> ValidLatinRomanChars = new() { 'I', 'V', 'X', 'L', 'C', 'D', 'M' };

	private static readonly Dictionary<string, string> EndingFixes = new(StringComparer.OrdinalIgnoreCase)
	{
		{"ого", "го"}, {"ому", "му"}, {"ий", "й"}, {"ій", "й"},
		{"им", "м"}, {"ім", "м"}, {"ою", "ю"}
	};

	private static readonly HashSet<string> ValidShortEndings = new(StringComparer.OrdinalIgnoreCase)
	{
		"й", "я", "ю", "ї", "м", "х", "го", "му", "ми", "а", "е", "є"
	};

	public override IReadOnlyCollection<RuleDefinition> SupportedRules { get; } = new List<RuleDefinition>
	{
		new(MissingSpaceUnitId, IssueCategory.Typography, IssueSeverity.Warning),
		new(MissingSpacePercentId, IssueCategory.Typography, IssueSeverity.Warning),
		new(MissingHyphenEndingId, IssueCategory.Grammar, IssueSeverity.Warning),
		new(WrongEndingId, IssueCategory.Grammar, IssueSeverity.Warning),
		new(SpacedRangeId, IssueCategory.Typography, IssueSeverity.Warning),
		new(WrongDashRangeId, IssueCategory.Typography, IssueSeverity.Warning),
		new(RomanCyrillicId, IssueCategory.Typography, IssueSeverity.Warning)
	};

	public NumberAndUnitAnalyzer(FstSuffixDictionary<UkMorphologyTag> dictionary)
	{
		_dictionary = dictionary;
	}

	protected override IEnumerable<TextIssue> Execute(Sentence sentence)
	{
		var tokens = sentence.Tokens;
		// Skip index allows us to jump over tokens we've already processed in a multi-token pattern (like ranges)
		int skipUntilIndex = -1;

		for (int i = 0; i < tokens.Count; i++)
		{
			if (i <= skipUntilIndex) continue;

			var token = tokens[i];

			if (token.Type == TokenType.Number)
			{
				// 1. Check for ranges with spaces: "1990 - 2000"
				var rangeResult = CheckSpacedRange(tokens, i);
				if (rangeResult.Consumed > 0)
				{
					if (rangeResult.Issue != null) yield return rangeResult.Issue;
					skipUntilIndex = i + rangeResult.Consumed;
					continue;
				}

				// 2. Check for tight adjacencies: "100%", "5й", "10-20"
				var adjacencyResult = CheckAdjacentTokens(tokens, i);
				if (adjacencyResult.Consumed > 0)
				{
					if (adjacencyResult.Issue != null) yield return adjacencyResult.Issue;
					skipUntilIndex = i + adjacencyResult.Consumed;
					continue;
				}
			}
			else if (token.Type == TokenType.Word)
			{
				// 3. Check for Roman numerals written with Cyrillic letters
				foreach (var issue in CheckRomanNumeral(token))
				{
					yield return issue;
				}
			}
		}
	}

	private (TextIssue? Issue, int Consumed) CheckSpacedRange(IReadOnlyList<Token> tokens, int i)
	{
		// Pattern: [Num] [Space] [Dash] [Space] [Num]
		// Indices: i      i+1     i+2    i+3     i+4
		if (i + 4 < tokens.Count &&
			tokens[i + 1].Type == TokenType.Whitespace &&
			tokens[i + 2].Type == TokenType.Punctuation && IsDash(tokens[i + 2].Text) &&
			tokens[i + 3].Type == TokenType.Whitespace &&
			tokens[i + 4].Type == TokenType.Number)
		{
			var chunk = new[] { tokens[i], tokens[i + 1], tokens[i + 2], tokens[i + 3], tokens[i + 4] };
			string suggestion = $"{tokens[i].Text}–{tokens[i + 4].Text}";

			// consumed = 4 means we skip the next 4 tokens in the main loop
			return (CreateIssue(chunk, SpacedRangeId, suggestion), 4);
		}

		return (null, 0);
	}

	private (TextIssue? Issue, int Consumed) CheckAdjacentTokens(IReadOnlyList<Token> tokens, int i)
	{
		if (i + 1 >= tokens.Count) return (null, 0);

		var current = tokens[i];
		var next = tokens[i + 1];

		// Must be physically touching (no whitespace in between)
		if (current.End != next.Start) return (null, 0);

		// Case A: Number + Symbol/Punctuation
		if (next.Type == TokenType.Punctuation || next.Type == TokenType.Symbol)
		{
			// "100%" -> "100 %"
			if (next.Text is "%" or "‰")
			{
				return (CreateIssue(new[] { current, next }, MissingSpacePercentId, $"{current.Text} {next.Text}"), 1);
			}

			// "10-20" -> "10–20" (Hyphen/Dash followed by Number)
			if (IsDash(next.Text) && i + 2 < tokens.Count)
			{
				var nextNext = tokens[i + 2];
				if (nextNext.Start == next.End && nextNext.Type == TokenType.Number)
				{
					// It's a range without spaces using a hyphen/dash
					return (CreateIssue(new[] { current, next, nextNext }, WrongDashRangeId, $"{current.Text}–{nextNext.Text}"), 2);
				}
			}
		}

		// Case B: Number + Word
		if (next.Type == TokenType.Word)
		{
			string word = next.Text;

			// "5ого" -> "5-го" (Wrong long ending)
			if (EndingFixes.TryGetValue(word, out string correctEnding))
			{
				return (CreateIssue(new[] { current, next }, WrongEndingId, $"{current.Text}-{correctEnding}"), 1);
			}

			// "5й" -> "5-й" (Missing hyphen for ordinal ending)
			if (ValidShortEndings.Contains(word))
			{
				return (CreateIssue(new[] { current, next }, MissingHyphenEndingId, $"{current.Text}-{word}"), 1);
			}

			// "100грн" -> "100 грн" (Missing unit space)
			// Default fallback: if it's a word touching a number, it's likely a unit
			return (CreateIssue(new[] { current, next }, MissingSpaceUnitId, $"{current.Text} {next.Text}"), 1);
		}

		return (null, 0);
	}

	private IEnumerable<TextIssue> CheckRomanNumeral(Token token)
	{
		// 1. Preliminary check: Must be all Uppercase
		if (token.Text.Length == 0 || !token.Text.All(char.IsUpper))
		{
			yield break;
		}

		// 2. Conversion & Validation loop
		bool hasCyrillic = false;
		bool hasInvalidChars = false;
		var sb = new StringBuilder(token.Text.Length);

		foreach (char c in token.Text)
		{
			if (CyrillicToLatinRoman.TryGetValue(c, out char latin))
			{
				hasCyrillic = true;
				sb.Append(latin);
			}
			else if (ValidLatinRomanChars.Contains(c))
			{
				sb.Append(c);
			}
			else
			{
				hasInvalidChars = true;
				break;
			}
		}

		// 3. Filtering
		if (hasInvalidChars || !hasCyrillic)
		{
			yield break;
		}

		string translated = sb.ToString();

		// 4. Strict Validation
		// - Must match Roman numeral pattern
		// - Must NOT be a valid word in the dictionary (e.g., "МІ" is a note, "ДІ" is "actions")
		if (RomanNumeralRegex.IsMatch(translated) && !_dictionary.Contains(token.NormalizedText))
		{
			yield return CreateIssue(token, RomanCyrillicId, translated);
		}
	}

	private static bool IsDash(string text) => text is "-" or "—" or "–";
}