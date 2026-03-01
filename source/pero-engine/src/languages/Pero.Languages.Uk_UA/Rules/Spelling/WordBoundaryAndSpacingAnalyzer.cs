using Pero.Abstractions.Contracts;
using Pero.Abstractions.Models;
using Pero.Kernel.Dictionaries;
using Pero.Kernel.Rules;

namespace Pero.Languages.Uk_UA.Rules.Spelling;

public class WordBoundaryAndSpacingAnalyzer : BaseAnalyzer
{
	public override string Name => "WordBoundaryAndSpacing";

	private const string GluedWordsId = "UK_UA_SPACING_GLUED_WORDS";
	private const string CamelCaseId = "UK_UA_SPACING_CAMEL_CASE";
	private const string SplitWordId = "UK_UA_SPACING_SPLIT_WORD";
	private const string PdfHyphenationId = "UK_UA_SPACING_PDF_HYPHENATION";
	private const string MultipleSpacesId = "UK_UA_SPACING_MULTIPLE_SPACES";
	private const string TrailingWhitespaceId = "UK_UA_SPACING_TRAILING_WHITESPACE";

	private readonly FstSuffixDictionary<UkMorphologyTag> _dictionary;

	public override IReadOnlyCollection<RuleDefinition> SupportedRules { get; } = new List<RuleDefinition>
	{
		new(GluedWordsId, IssueCategory.Spelling, IssueSeverity.Warning),
		new(CamelCaseId, IssueCategory.Spelling, IssueSeverity.Warning),
		new(SplitWordId, IssueCategory.Spelling, IssueSeverity.Warning),
		new(PdfHyphenationId, IssueCategory.Spelling, IssueSeverity.Warning),
		new(MultipleSpacesId, IssueCategory.Typography, IssueSeverity.Warning),
		new(TrailingWhitespaceId, IssueCategory.Typography, IssueSeverity.Warning)
	};

	public WordBoundaryAndSpacingAnalyzer(FstSuffixDictionary<UkMorphologyTag> dictionary)
	{
		_dictionary = dictionary;
	}

	protected override IEnumerable<TextIssue> Execute(Sentence sentence)
	{
		var tokens = sentence.Tokens;
		int skipUntilIndex = -1;

		for (int i = 0; i < tokens.Count; i++)
		{
			if (i <= skipUntilIndex) continue;

			var token = tokens[i];

			if (token.Type == TokenType.Whitespace)
			{
				foreach (var issue in AnalyzeWhitespace(tokens, i)) yield return issue;
				continue;
			}

			if (token.Type == TokenType.Word)
			{
				var singleWordIssue = AnalyzeSingleWord(token);
				if (singleWordIssue != null)
				{
					yield return singleWordIssue;
					continue;
				}
			}

			var multiTokenResult = AnalyzeMultiTokenPatterns(tokens, i);
			if (multiTokenResult.Consumed > 0)
			{
				if (multiTokenResult.Issue != null) yield return multiTokenResult.Issue;
				skipUntilIndex = i + multiTokenResult.Consumed - 1;
			}
		}
	}

	private IEnumerable<TextIssue> AnalyzeWhitespace(IReadOnlyList<Token> tokens, int currentIndex)
	{
		var token = tokens[currentIndex];

		if (token.Text.Contains("  "))
		{
			string cleaned = token.Text.Replace("  ", " ");
			while (cleaned.Contains("  ")) cleaned = cleaned.Replace("  ", " ");

			yield return CreateContextualIssue(tokens, currentIndex, MultipleSpacesId, cleaned);
		}

		if (token.Text.Contains(" \n") || token.Text.Contains("\t\n"))
		{
			string cleaned = token.Text.Replace(" \n", "\n").Replace("\t\n", "\n");

			yield return CreateContextualIssue(tokens, currentIndex, TrailingWhitespaceId, cleaned);
		}
	}

	private TextIssue CreateContextualIssue(IReadOnlyList<Token> tokens, int targetIndex, string ruleId, string cleanedCenter)
	{
		var prev = targetIndex > 0 ? tokens[targetIndex - 1] : null;
		var next = targetIndex + 1 < tokens.Count ? tokens[targetIndex + 1] : null;

		var chunk = new List<Token>();
		if (prev != null) chunk.Add(prev);
		chunk.Add(tokens[targetIndex]);
		if (next != null) chunk.Add(next);

		string suggestion = (prev?.Text ?? string.Empty) + cleanedCenter + (next?.Text ?? string.Empty);

		return CreateIssue(chunk, ruleId, suggestion);
	}

	private TextIssue? AnalyzeSingleWord(Token token)
	{
		if (TryDetectCamelCase(token, out var ccIssue)) return ccIssue;
		if (TryDetectGluedWords(token, out var gwIssue)) return gwIssue;

		return null;
	}

	private bool TryDetectCamelCase(Token token, out TextIssue? issue)
	{
		issue = null;
		string text = token.Text;
		if (text.Length < 3) return false;

		for (int i = 0; i < text.Length - 1; i++)
		{
			if (char.IsLower(text[i]) && char.IsUpper(text[i + 1]) && IsCyrillic(text[i]) && IsCyrillic(text[i + 1]))
			{
				string suggestion = $"{text.Substring(0, i + 1)} {text.Substring(i + 1).ToLowerInvariant()}";
				issue = CreateIssue(token, CamelCaseId, suggestion);
				return true;
			}
		}

		return false;
	}

	private bool TryDetectGluedWords(Token token, out TextIssue? issue)
	{
		issue = null;

		if (token.Text.Length < 6) return false;
		if (_dictionary.Contains(token.NormalizedText)) return false;

		var span = token.NormalizedText.AsSpan();

		for (int split = 3; split <= span.Length - 3; split++)
		{
			var left = span[..split];
			var right = span[split..];

			if (_dictionary.Contains(left) && _dictionary.Contains(right))
			{
				string rawLeft = token.Text.Substring(0, split);
				string rawRight = token.Text.Substring(split);
				string suggestion = $"{rawLeft} {rawRight}";

				issue = CreateIssue(token, GluedWordsId, suggestion);
				return true;
			}
		}

		return false;
	}

	private (TextIssue? Issue, int Consumed) AnalyzeMultiTokenPatterns(IReadOnlyList<Token> tokens, int startIndex)
	{
		if (TryDetectSplitWord(tokens, startIndex, out var splitIssue))
		{
			return (splitIssue, 3);
		}

		if (TryDetectPdfHyphenation(tokens, startIndex, out var pdfIssue))
		{
			return (pdfIssue, 4);
		}

		return (null, 0);
	}

	private bool TryDetectSplitWord(IReadOnlyList<Token> tokens, int i, out TextIssue? issue)
	{
		issue = null;

		if (i + 2 >= tokens.Count) return false;

		var t1 = tokens[i];
		var t2 = tokens[i + 1];
		var t3 = tokens[i + 2];

		if (t1.Type != TokenType.Word || t3.Type != TokenType.Word) return false;
		if (t2.Type != TokenType.Whitespace || t2.Text != " ") return false;

		bool leftValid = _dictionary.Contains(t1.NormalizedText);
		bool rightValid = _dictionary.Contains(t3.NormalizedText);

		if (leftValid && rightValid) return false;

		string combined = t1.NormalizedText + t3.NormalizedText;
		if (combined.Length >= 4 && _dictionary.Contains(combined))
		{
			string suggestion = MatchCapitalization(combined, t1.Text);
			issue = CreateIssue(new[] { t1, t2, t3 }, SplitWordId, suggestion);
			return true;
		}

		return false;
	}

	private bool TryDetectPdfHyphenation(IReadOnlyList<Token> tokens, int i, out TextIssue? issue)
	{
		issue = null;

		if (i + 3 >= tokens.Count) return false;

		var t1 = tokens[i];
		var t2 = tokens[i + 1];
		var t3 = tokens[i + 2];
		var t4 = tokens[i + 3];

		if (t1.Type != TokenType.Word || t4.Type != TokenType.Word) return false;
		if (t2.Type != TokenType.Punctuation || (t2.Text != "-" && t2.Text != "—")) return false;
		if (t3.Type != TokenType.Whitespace || !t3.Text.Contains('\n')) return false;

		string combined = t1.NormalizedText + t4.NormalizedText;
		if (_dictionary.Contains(combined))
		{
			string suggestion = MatchCapitalization(combined, t1.Text);
			issue = CreateIssue(new[] { t1, t2, t3, t4 }, PdfHyphenationId, suggestion);
			return true;
		}

		return false;
	}

	private static bool IsCyrillic(char c)
	{
		return (c >= '\u0400' && c <= '\u04FF') || (c >= '\u0500' && c <= '\u052F') || c == 'і' || c == 'ї' || c == 'є' || c == 'ґ';
	}

	private static string MatchCapitalization(string suggestion, string originalReference)
	{
		if (string.IsNullOrEmpty(originalReference) || string.IsNullOrEmpty(suggestion)) return suggestion;

		bool isFirstUpper = char.IsUpper(originalReference[0]);
		if (isFirstUpper) return char.ToUpperInvariant(suggestion[0]) + suggestion[1..];

		return suggestion;
	}
}