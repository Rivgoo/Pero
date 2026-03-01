using System.Text;
using Pero.Abstractions.Contracts;
using Pero.Abstractions.Models;
using Pero.Kernel.Dictionaries;
using Pero.Kernel.Rules;

namespace Pero.Languages.Uk_UA.Rules.Spelling;

public class OcrAndLayoutArtifactAnalyzer : BaseAnalyzer
{
	public override string Name => "OcrAndLayoutArtifacts";

	private const string LayoutId = "UK_UA_ARTIFACT_WRONG_LAYOUT";
	private const string HomoglyphId = "UK_UA_ARTIFACT_HOMOGLYPH";
	private const string OcrSubstitutionId = "UK_UA_ARTIFACT_OCR_DIGIT";
	private const string StrayIntrusionId = "UK_UA_ARTIFACT_STRAY_SYMBOL";
	private const string SpacedWordId = "UK_UA_ARTIFACT_SPACED_WORD";

	private readonly FstSuffixDictionary<UkMorphologyTag> _dictionary;

	private static readonly Dictionary<char, char> HomoglyphMap = new()
	{
		{'a', 'а'}, {'o', 'о'}, {'e', 'е'}, {'i', 'і'}, {'p', 'р'}, {'c', 'с'}, {'x', 'х'}, {'y', 'у'},
		{'A', 'А'}, {'O', 'О'}, {'E', 'Е'}, {'I', 'І'}, {'P', 'Р'}, {'C', 'С'}, {'X', 'Х'}, {'Y', 'У'}
	};

	private static readonly Dictionary<char, char> LayoutMap = new()
	{
		{'q', 'й'}, {'w', 'ц'}, {'e', 'у'}, {'r', 'к'}, {'t', 'е'}, {'y', 'н'}, {'u', 'г'}, {'i', 'ш'}, {'o', 'щ'}, {'p', 'з'}, {'[', 'х'}, {']', 'ї'},
		{'a', 'ф'}, {'s', 'і'}, {'d', 'в'}, {'f', 'а'}, {'g', 'п'}, {'h', 'р'}, {'j', 'о'}, {'k', 'л'}, {'l', 'д'}, {';', 'ж'}, {'\'', 'є'},
		{'z', 'я'}, {'x', 'ч'}, {'c', 'с'}, {'v', 'м'}, {'b', 'и'}, {'n', 'т'}, {'m', 'ь'}, {',', 'б'}, {'.', 'ю'},
		{'Q', 'Й'}, {'W', 'Ц'}, {'E', 'У'}, {'R', 'К'}, {'T', 'Е'}, {'Y', 'Н'}, {'U', 'Г'}, {'I', 'Ш'}, {'O', 'Щ'}, {'P', 'З'}, {'{', 'Х'}, {'}', 'Ї'},
		{'A', 'Ф'}, {'S', 'І'}, {'D', 'В'}, {'F', 'А'}, {'G', 'П'}, {'H', 'Р'}, {'J', 'О'}, {'K', 'Л'}, {'L', 'Д'}, {':', 'Ж'}, {'"', 'Є'},
		{'Z', 'Я'}, {'X', 'Ч'}, {'C', 'С'}, {'V', 'М'}, {'B', 'И'}, {'N', 'Т'}, {'M', 'Ь'}, {'<', 'Б'}, {'>', 'Ю'}
	};

	public override IReadOnlyCollection<RuleDefinition> SupportedRules { get; } = new List<RuleDefinition>
	{
		new(LayoutId, IssueCategory.Spelling, IssueSeverity.Warning),
		new(HomoglyphId, IssueCategory.Spelling, IssueSeverity.Warning),
		new(OcrSubstitutionId, IssueCategory.Spelling, IssueSeverity.Warning),
		new(StrayIntrusionId, IssueCategory.Spelling, IssueSeverity.Warning),
		new(SpacedWordId, IssueCategory.Spelling, IssueSeverity.Warning)
	};

	public OcrAndLayoutArtifactAnalyzer(FstSuffixDictionary<UkMorphologyTag> dictionary)
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

			var spacedResult = CheckSpacedWord(tokens, i);
			if (spacedResult.Consumed > 0)
			{
				if (spacedResult.Issue != null) yield return spacedResult.Issue;
				skipUntilIndex = i + spacedResult.Consumed - 1;
				continue;
			}

			int endIndex = i;
			while (endIndex + 1 < tokens.Count && tokens[endIndex].End == tokens[endIndex + 1].Start)
			{
				endIndex++;
			}

			var chunk = tokens.Skip(i).Take(endIndex - i + 1).ToList();
			var coreChunk = GetCoreChunk(chunk);

			if (coreChunk.Count > 0)
			{
				var artifactIssue = CheckChunkArtifacts(coreChunk);
				if (artifactIssue != null)
				{
					yield return artifactIssue;
				}
			}

			skipUntilIndex = endIndex;
		}
	}

	private IReadOnlyList<Token> GetCoreChunk(IReadOnlyList<Token> chunk)
	{
		int start = 0;
		while (start < chunk.Count && IsBoundaryToken(chunk[start])) start++;

		int end = chunk.Count - 1;
		while (end >= start && IsBoundaryToken(chunk[end])) end--;

		if (start > end) return Array.Empty<Token>();

		return chunk.Skip(start).Take(end - start + 1).ToList();
	}

	private static bool IsBoundaryToken(Token token)
	{
		if (token.Type == TokenType.Word || token.Type == TokenType.Number) return false;
		if (token.Text == "'" || token.Text == "’" || token.Text == "ʼ") return false;
		return true;
	}

	private (TextIssue? Issue, int Consumed) CheckSpacedWord(IReadOnlyList<Token> tokens, int startIndex)
	{
		if (tokens[startIndex].Type != TokenType.Word || tokens[startIndex].Text.Length != 1) return (null, 0);

		var sb = new StringBuilder(tokens[startIndex].Text);
		int currentIndex = startIndex;

		while (currentIndex + 2 < tokens.Count &&
			   tokens[currentIndex + 1].Type == TokenType.Whitespace &&
			   tokens[currentIndex + 2].Type == TokenType.Word &&
			   tokens[currentIndex + 2].Text.Length == 1 &&
			   char.IsLetter(tokens[currentIndex + 2].Text[0]))
		{
			if (currentIndex + 3 < tokens.Count && tokens[currentIndex + 3].Text == ".") break;

			sb.Append(tokens[currentIndex + 2].Text);
			currentIndex += 2;
		}

		int lettersCount = (currentIndex - startIndex) / 2 + 1;

		if (lettersCount >= 4)
		{
			string candidate = sb.ToString();
			if (_dictionary.Contains(candidate.ToLowerInvariant()))
			{
				int consumed = currentIndex - startIndex + 1;
				string suggestion = MatchCapitalization(candidate, tokens[startIndex].Text);
				var issue = CreateIssue(tokens.Skip(startIndex).Take(consumed).ToList(), SpacedWordId, suggestion);

				return (issue, consumed);
			}
		}

		return (null, 0);
	}

	private TextIssue? CheckChunkArtifacts(IReadOnlyList<Token> chunk)
	{
		string originalText = string.Join("", chunk.Select(t => t.Text));
		if (originalText.Length < 2) return null;

		if (IsEntirelyLatinLayout(originalText))
		{
			string mapped = MapLayout(originalText);
			if (_dictionary.Contains(mapped.ToLowerInvariant()))
			{
				return CreateIssue(chunk, LayoutId, mapped);
			}
		}

		if (HasMixedScripts(originalText))
		{
			if (TryMapHomoglyphs(originalText, out string mapped) && _dictionary.Contains(mapped.ToLowerInvariant()))
			{
				string suggestion = MatchCapitalization(mapped, originalText);
				return CreateIssue(chunk, HomoglyphId, suggestion);
			}
		}

		bool hasLetters = originalText.Any(char.IsLetter);
		bool hasNumbers = chunk.Any(t => t.Type == TokenType.Number);

		if (hasLetters && hasNumbers)
		{
			if (TryMapOcrDigits(originalText, out string mapped))
			{
				string suggestion = MatchCapitalization(mapped, originalText);
				return CreateIssue(chunk, OcrSubstitutionId, suggestion);
			}
		}

		if (chunk.Count >= 3 && chunk[0].Type == TokenType.Word && chunk[^1].Type == TokenType.Word)
		{
			var sb = new StringBuilder();
			foreach (var t in chunk)
			{
				if (t.Type == TokenType.Word) sb.Append(t.Text);
			}

			string cleaned = sb.ToString();
			if (cleaned.Length > 2 && cleaned.Length < originalText.Length && _dictionary.Contains(cleaned.ToLowerInvariant()))
			{
				string suggestion = MatchCapitalization(cleaned, originalText);
				return CreateIssue(chunk, StrayIntrusionId, suggestion);
			}
		}

		return null;
	}

	private bool TryMapOcrDigits(string text, out string result)
	{
		result = string.Empty;
		var sb = new StringBuilder(text);
		bool hasReplacements = false;

		for (int i = 0; i < sb.Length; i++)
		{
			if (!char.IsDigit(sb[i])) continue;

			bool isUpper = i > 0 ? char.IsUpper(text[i - 1]) : (i < text.Length - 1 && char.IsUpper(text[i + 1]));

			switch (sb[i])
			{
				case '0': sb[i] = isUpper ? 'О' : 'о'; hasReplacements = true; break;
				case '3': sb[i] = isUpper ? 'З' : 'з'; hasReplacements = true; break;
				case '4': sb[i] = isUpper ? 'Ч' : 'ч'; hasReplacements = true; break;
				case '6': sb[i] = isUpper ? 'Б' : 'б'; hasReplacements = true; break;
				case '8': sb[i] = isUpper ? 'В' : 'в'; hasReplacements = true; break;
			}
		}

		if (!hasReplacements && !text.Contains('1')) return false;

		string candidate = sb.ToString();
		if (!candidate.Contains('1'))
		{
			if (_dictionary.Contains(candidate.ToLowerInvariant()))
			{
				result = candidate;
				return true;
			}
			return false;
		}

		var s1 = candidate.Replace('1', 'і');
		if (_dictionary.Contains(s1.ToLowerInvariant())) { result = s1; return true; }

		var s2 = candidate.Replace('1', 'л');
		if (_dictionary.Contains(s2.ToLowerInvariant())) { result = s2; return true; }

		return false;
	}

	private static bool IsEntirelyLatinLayout(string text)
	{
		bool hasLatinLetters = false;
		foreach (char c in text)
		{
			if (char.IsLetter(c))
			{
				if (IsLatin(c)) hasLatinLetters = true;
				else return false;
			}
		}
		return hasLatinLetters;
	}

	private static string MapLayout(string text)
	{
		var sb = new StringBuilder(text.Length);
		foreach (char c in text)
		{
			sb.Append(LayoutMap.TryGetValue(c, out char mapped) ? mapped : c);
		}
		return sb.ToString();
	}

	private static bool HasMixedScripts(string text)
	{
		bool hasCyrillic = false;
		bool hasLatin = false;
		foreach (char c in text)
		{
			if (IsCyrillic(c)) hasCyrillic = true;
			else if (IsLatin(c)) hasLatin = true;
		}
		return hasCyrillic && hasLatin;
	}

	private static bool TryMapHomoglyphs(string text, out string mapped)
	{
		mapped = string.Empty;
		var sb = new StringBuilder(text.Length);
		bool hasReplacements = false;

		foreach (char c in text)
		{
			if (IsLatin(c))
			{
				if (HomoglyphMap.TryGetValue(c, out char cyr))
				{
					sb.Append(cyr);
					hasReplacements = true;
				}
				else return false;
			}
			else
			{
				sb.Append(c);
			}
		}

		if (!hasReplacements) return false;
		mapped = sb.ToString();
		return true;
	}

	private static string MatchCapitalization(string suggestion, string original)
	{
		if (string.IsNullOrEmpty(original) || string.IsNullOrEmpty(suggestion)) return suggestion;

		bool isAllUpper = original.Where(char.IsLetter).All(char.IsUpper);
		if (isAllUpper && original.Any(char.IsLetter)) return suggestion.ToUpperInvariant();

		if (char.IsLetter(original[0]) && char.IsUpper(original[0]))
			return char.ToUpperInvariant(suggestion[0]) + suggestion[1..];

		return suggestion;
	}

	private static bool IsCyrillic(char c) => (c >= '\u0400' && c <= '\u04FF') || (c >= '\u0500' && c <= '\u052F');
	private static bool IsLatin(char c) => (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z');
}