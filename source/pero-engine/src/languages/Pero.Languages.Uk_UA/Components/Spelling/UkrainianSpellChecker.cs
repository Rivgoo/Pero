using Pero.Abstractions.Contracts;
using Pero.Abstractions.Models;
using Pero.Abstractions.Telemetry;
using Pero.Kernel.Fuzzy;
using Pero.Kernel.Ngrams;
using Pero.Kernel.Utils;
using Pero.Languages.Uk_UA.Components.Caching;
using Pero.Languages.Uk_UA.Components.Spelling.Context;
using Pero.Languages.Uk_UA.Extensions;

namespace Pero.Languages.Uk_UA.Components;

public class UkrainianSpellChecker : ISpellChecker
{
	private const string IssueId = "UK_UA_SPELLING_ERROR";
	private const string UnknownWordIssueId = "UK_UA_SPELLING_UNKNOWN_WORD";
	private const char CanonicalApostrophe = '\'';
	private const int MinCandidatesForEarlyExit = 3;

	private readonly FuzzyMatcher<UkMorphologyTag> fuzzyMatcher;
	private readonly VirtualSymSpell<UkMorphologyTag> virtualSymSpell;
	private readonly LexiconCache lexicon;
	private readonly NgramLanguageModel ngramModel;
	private readonly IReadOnlyList<ISpellingHeuristic> heuristics;

	public UkrainianSpellChecker(
		FuzzyMatcher<UkMorphologyTag> fuzzyMatcher,
		VirtualSymSpell<UkMorphologyTag> virtualSymSpell,
		LexiconCache lexicon,
		NgramLanguageModel ngramModel,
		IEnumerable<ISpellingHeuristic> heuristics)
	{
		this.fuzzyMatcher = fuzzyMatcher;
		this.virtualSymSpell = virtualSymSpell;
		this.lexicon = lexicon;
		this.ngramModel = ngramModel;
		this.heuristics = heuristics.ToList();
	}

	public IEnumerable<TextIssue> Check(AnalyzedDocument document, ITelemetryTracker telemetry)
	{
		DocumentSessionCache sessionCache;
		using (telemetry.Measure("SpellCheck.SessionCacheInit"))
		{
			sessionCache = new DocumentSessionCache(document);
		}

		var contextRanker = new ContextRanker(sessionCache, ngramModel);

		foreach (var sentence in document.Sentences)
		{
			foreach (var token in sentence.Tokens)
			{
				if (token.Type != TokenType.Word || !token.IsUnknown()) continue;

				string normalizedText;
				bool hasHomoglyphs;

				using (telemetry.Measure("SpellCheck.StringNormalization"))
				{
					normalizedText = NormalizeApostrophes(token.NormalizedText);
					hasHomoglyphs = HasLatinHomoglyphs(token.Text);
				}

				if (!hasHomoglyphs && ContainsNonUkrainianChars(normalizedText)) continue;

				var combinedCandidates = new List<CorrectionCandidate<UkMorphologyTag>>();
				string searchTarget = hasHomoglyphs ? CleanHomoglyphs(normalizedText) : normalizedText;
				char userApostropheStyle = DetectUserApostrophe(token.Text);

				if (hasHomoglyphs)
				{
					TryAddDictCandidates(searchTarget, combinedCandidates, 0f, 2.0f);
				}

				if (combinedCandidates.Count == 0 && searchTarget.Length >= 4)
				{
					foreach (var splitCandidate in virtualSymSpell.GetSplitCandidates(searchTarget))
					{
						combinedCandidates.Add(splitCandidate);
					}
				}

				using (telemetry.Measure("SpellCheck.HeuristicsGeneration"))
				{
					foreach (var heuristic in heuristics)
					{
						foreach (var heuristicWord in heuristic.Generate(searchTarget))
						{
							TryAddDictCandidates(heuristicWord, combinedCandidates, 0.5f, 5.0f);
						}
					}
				}

				if (combinedCandidates.Count < MinCandidatesForEarlyExit)
				{
					using (telemetry.Measure("SpellCheck.SymSpell"))
					{
						foreach (var ed1 in virtualSymSpell.GetCandidates(searchTarget))
						{
							if (!combinedCandidates.Any(c => c.Word == ed1.Word))
								combinedCandidates.Add(ed1);
						}
					}
				}

				//if (combinedCandidates.Count == 0)
				//{
				//	using (telemetry.Measure("SpellCheck.FuzzyMatcher"))
				//	{
				//		var deepCandidates = fuzzyMatcher.Suggest(searchTarget);
				//		if (deepCandidates.Length > 0)
				//			combinedCandidates.AddRange(deepCandidates);
				//	}
				//}

				IReadOnlyList<CorrectionCandidate<UkMorphologyTag>> rankedCandidates;
				using (telemetry.Measure("SpellCheck.ContextRanking"))
				{
					rankedCandidates = contextRanker.Rank(sentence, token, combinedCandidates);
				}

				List<string> suggestions;
				using (telemetry.Measure("SpellCheck.SuggestionFormatting"))
				{
					suggestions = rankedCandidates
						.Take(5)
						.Select(c => MatchCapitalization(RestoreApostropheStyle(c.Word, userApostropheStyle), token.Text))
						.Distinct()
						.ToList();
				}

				var messageArgs = new Dictionary<string, string> { { "word", token.Text } };

				if (suggestions.Count > 0)
				{
					yield return IssueFactory.CreateFrom(token, IssueId, IssueCategory.Spelling, suggestions, messageArgs);
				}
				else
				{
					yield return IssueFactory.CreateFrom(token, UnknownWordIssueId, IssueCategory.Spelling, null, messageArgs);
				}
			}
		}
	}

	private void TryAddDictCandidates(string word, List<CorrectionCandidate<UkMorphologyTag>> pool, float distance, float bonus)
	{
		var dictTags = lexicon.GetCandidates(word);
		if (dictTags.Count > 0)
		{
			float score = distance - bonus - 10.0f;
			var typedTags = dictTags.Select(tg => tg.Tag as UkMorphologyTag).Where(t => t != null).ToArray();
			pool.Add(new CorrectionCandidate<UkMorphologyTag>(word, distance, 31, score, typedTags!));
		}
	}

	private static bool HasLatinHomoglyphs(string word) => word.Any(c => c is 'a' or 'o' or 'e' or 'i' or 'p' or 'c' or 'x' or 'y');

	private static string CleanHomoglyphs(string word)
	{
		return word.Replace('a', 'а').Replace('o', 'о').Replace('e', 'е').Replace('i', 'і')
				   .Replace('p', 'р').Replace('c', 'с').Replace('x', 'х').Replace('y', 'у');
	}

	private static bool ContainsNonUkrainianChars(string word)
	{
		foreach (char c in word)
		{
			if (c >= 'а' && c <= 'щ') continue;
			if (c >= 'ю' && c <= 'я') continue;
			if (c is 'ь' or 'і' or 'ї' or 'є' or 'ґ' or '\'' or '’' or 'ʼ' or '-') continue;
			return true;
		}
		return false;
	}

	private static string NormalizeApostrophes(string word) => word.Replace('’', CanonicalApostrophe).Replace('ʼ', CanonicalApostrophe);
	private static char DetectUserApostrophe(string word) => word.Contains('’') ? '’' : (word.Contains('ʼ') ? 'ʼ' : CanonicalApostrophe);

	private static string RestoreApostropheStyle(string suggestion, char userApostrophe)
	{
		return userApostrophe == CanonicalApostrophe ? suggestion : suggestion.Replace(CanonicalApostrophe, userApostrophe);
	}

	private static string MatchCapitalization(string suggestion, string original)
	{
		if (string.IsNullOrEmpty(original) || string.IsNullOrEmpty(suggestion)) return suggestion;

		bool isAllUpper = original.All(c => !char.IsLetter(c) || char.IsUpper(c));
		if (isAllUpper) return suggestion.ToUpperInvariant();

		if (char.IsUpper(original[0])) return char.ToUpperInvariant(suggestion[0]) + suggestion[1..];

		return suggestion;
	}
}