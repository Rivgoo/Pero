using Pero.Abstractions.Contracts;
using Pero.Abstractions.Models;
using Pero.Kernel.Utils;
using Pero.Languages.Uk_UA.Components.Spelling.Context;
using Pero.Languages.Uk_UA.Dictionaries.Fuzzy;

namespace Pero.Languages.Uk_UA.Components;

public partial class UkrainianSpellChecker : ISpellChecker
{
	private const string IssueId = "UK_UA_SPELLING_ERROR";
	private const char CanonicalApostrophe = '\'';
	private readonly FuzzyMatcher _fuzzyMatcher;

	public UkrainianSpellChecker(FuzzyMatcher fuzzyMatcher)
	{
		_fuzzyMatcher = fuzzyMatcher;
	}

	public IEnumerable<TextIssue> Check(AnalyzedDocument document)
	{
		var sessionCache = new DocumentSessionCache(document);
		var contextRanker = new ContextRanker(sessionCache);

		foreach (var sentence in document.Sentences)
		{
			foreach (var token in sentence.Tokens)
			{
				if (IsTechnical(token)) continue;

				if (token.IsUnknown())
				{
					if (ShouldSkipSpellcheck(token.Text)) continue;

					string searchTarget = NormalizeApostrophes(token.NormalizedText);
					char userApostropheStyle = DetectUserApostrophe(token.Text);

					var candidates = _fuzzyMatcher.Suggest(searchTarget, maxDistance: 2.0f);
					if (candidates.Count == 0) continue;

					var rankedCandidates = contextRanker.Rank(sentence, token, candidates);

					var suggestions = rankedCandidates
						.Take(5)
						.Select(c =>
						{
							string styled = RestoreApostropheStyle(c.Word, userApostropheStyle);
							return MatchCapitalization(styled, token.Text);
						})
						.Distinct()
						.ToList();

					if (suggestions.Count > 0)
					{
						yield return IssueFactory.CreateFrom(token, IssueId, IssueCategory.Spelling, suggestions);
					}
				}
			}
		}
	}

	private static bool ShouldSkipSpellcheck(string word)
	{
		if (string.IsNullOrWhiteSpace(word)) return true;

		foreach (char c in word)
		{
			if (char.IsDigit(c)) return true;
			if (char.IsLetter(c) && (c < '\u0400' || c > '\u04FF')) return true;
		}

		return false;
	}

	private static string NormalizeApostrophes(string word)
	{
		if (!word.Contains('’') && !word.Contains('ʼ')) return word;
		return word.Replace('’', CanonicalApostrophe).Replace('ʼ', CanonicalApostrophe);
	}

	private static char DetectUserApostrophe(string originalWord)
	{
		if (originalWord.Contains('’')) return '’';
		if (originalWord.Contains('ʼ')) return 'ʼ';
		return CanonicalApostrophe;
	}

	private static string RestoreApostropheStyle(string suggestion, char userApostrophe)
	{
		if (userApostrophe == CanonicalApostrophe || !suggestion.Contains(CanonicalApostrophe)) return suggestion;
		return suggestion.Replace(CanonicalApostrophe, userApostrophe);
	}

	private static bool IsTechnical(Token token)
	{
		return token.Type switch
		{
			TokenType.Url => true,
			TokenType.Email => true,
			TokenType.CodeSnippet => true,
			TokenType.FilePath => true,
			TokenType.IpAddress => true,
			TokenType.Guid => true,
			_ => false
		};
	}

	private static string MatchCapitalization(string suggestion, string original)
	{
		if (string.IsNullOrEmpty(original) || string.IsNullOrEmpty(suggestion)) return suggestion;

		bool isAllUpper = original.All(c => !char.IsLetter(c) || char.IsUpper(c));
		if (isAllUpper) return suggestion.ToUpperInvariant();

		bool isFirstUpper = char.IsUpper(original[0]);
		if (isFirstUpper) return char.ToUpperInvariant(suggestion[0]) + suggestion.Substring(1);

		return suggestion;
	}
}