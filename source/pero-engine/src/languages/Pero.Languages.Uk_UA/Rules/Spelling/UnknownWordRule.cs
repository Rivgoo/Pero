using Pero.Abstractions.Models;
using Pero.Kernel.Rules;
using Pero.Kernel.Utils;
using Pero.Languages.Uk_UA.Dictionaries.Fuzzy;

namespace Pero.Languages.Uk_UA.Rules.Spelling;

/// <summary>
/// Detects words that are not present in the dictionary and provides spelling suggestions.
/// </summary>
public class UnknownWordRule : BaseGrammarRule
{
	private readonly FuzzyMatcher _fuzzyMatcher;

	public UnknownWordRule(FuzzyMatcher fuzzyMatcher)
	{
		_fuzzyMatcher = fuzzyMatcher;
	}

	public override string Id => "UK_UA_SPELLING_UNKNOWN_WORD";
	public override IssueCategory Category => IssueCategory.Spelling;
	public override IssueSeverity Severity => IssueSeverity.Warning;

	protected override IEnumerable<TextIssue> Analyze(Sentence sentence)
	{
		foreach (var token in sentence.Tokens)
		{
			if (IsTechnical(token)) continue;

			if (token.IsUnknown())
			{
				// Word is completely unknown. Trigger the heavy fuzzy search.
				var candidates = _fuzzyMatcher.Suggest(token.NormalizedText, maxDistance: 2);

				var suggestions = candidates
					.Select(c => MatchCapitalization(c.Word, token.Text))
					.ToList();

				yield return IssueFactory.CreateFrom(
					token,
					Id,
					Category,
					suggestions
				);
			}
		}
	}

	/// <summary>
	/// Restores the original capitalization format to the suggested lowercase lemma.
	/// </summary>
	private static string MatchCapitalization(string suggestion, string original)
	{
		if (string.IsNullOrEmpty(original) || string.IsNullOrEmpty(suggestion))
			return suggestion;

		bool isFirstUpper = char.IsUpper(original[0]);
		bool isAllUpper = original.All(c => !char.IsLetter(c) || char.IsUpper(c));

		if (isAllUpper)
		{
			return suggestion.ToUpperInvariant();
		}

		if (isFirstUpper)
		{
			return char.ToUpperInvariant(suggestion[0]) + suggestion.Substring(1);
		}

		return suggestion;
	}
}