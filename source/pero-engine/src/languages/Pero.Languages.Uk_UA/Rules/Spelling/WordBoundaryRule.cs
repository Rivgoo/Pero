using System.Text;
using Pero.Abstractions.Models;
using Pero.Kernel.Dictionaries;
using Pero.Kernel.Rules;

namespace Pero.Languages.Uk_UA.Rules.Spelling;

public class WordBoundaryRule : BaseGrammarRule
{
	public override string Id => "UK_UA_SPELLING_INVALID_BOUNDARY";
	public override IssueCategory Category => IssueCategory.Spelling;
	public override IssueSeverity Severity => IssueSeverity.Warning;

	private readonly FstSuffixDictionary<UkMorphologyTag> dictionary;

	public WordBoundaryRule(FstSuffixDictionary<UkMorphologyTag> dictionary)
	{
		this.dictionary = dictionary;
	}

	protected override IEnumerable<TextIssue> Analyze(Sentence sentence)
	{
		var tokens = sentence.Tokens;

		for (int i = 0; i < tokens.Count; i++)
		{
			if (tokens[i].Type == TokenType.Whitespace) continue;

			int endIndex = i;
			bool hasGarbage = false;
			bool hasWord = false;

			while (endIndex < tokens.Count)
			{
				var current = tokens[endIndex];

				if (current.Type == TokenType.Word) hasWord = true;
				if (current.Type == TokenType.Number || current.Type == TokenType.Symbol) hasGarbage = true;

				if (endIndex + 1 < tokens.Count && current.End == tokens[endIndex + 1].Start)
				{
					endIndex++;
				}
				else
				{
					break;
				}
			}

			if (hasWord && hasGarbage && endIndex > i)
			{
				var rawSpan = tokens.Skip(i).Take(endIndex - i + 1).ToList();
				string originalText = string.Join("", rawSpan.Select(t => t.Text));
				string cleanText = StripToUaLetters(originalText);

				var suggestions = new List<string>();

				if (cleanText.Length > 0 && dictionary.Analyze(cleanText.ToLowerInvariant()).Any())
				{
					suggestions.Add(MatchCapitalization(cleanText, originalText));
				}

				yield return new TextIssue
				{
					RuleId = Id,
					Category = Category,
					Severity = Severity,
					Start = rawSpan.First().Start,
					End = rawSpan.Last().End,
					Original = originalText,
					Suggestions = suggestions
				};
			}

			i = endIndex;
		}
	}

	private static string StripToUaLetters(string text)
	{
		var sb = new StringBuilder(text.Length);
		foreach (char c in text)
		{
			if (char.IsLetter(c) || c == '\'' || c == '’' || c == 'ʼ')
			{
				sb.Append(c);
			}
		}
		return sb.ToString();
	}

	private static string MatchCapitalization(string suggestion, string original)
	{
		if (string.IsNullOrEmpty(original) || string.IsNullOrEmpty(suggestion)) return suggestion;

		bool isFirstUpper = char.IsUpper(original[0]);
		if (isFirstUpper) return char.ToUpperInvariant(suggestion[0]) + suggestion[1..];

		return suggestion;
	}
}