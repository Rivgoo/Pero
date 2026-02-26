using System.Diagnostics;
using System.Text.RegularExpressions;
using Pero.Abstractions.Contracts;
using Pero.Abstractions.Models;
using Pero.Kernel.Utils;
using Pero.Languages.Uk_UA.Components.Caching;
using Pero.Languages.Uk_UA.Components.Spelling.Context;
using Pero.Languages.Uk_UA.Dictionaries.Fuzzy;
using Pero.Languages.Uk_UA.Dictionaries.Ngrams;

namespace Pero.Languages.Uk_UA.Components;

public class UkrainianSpellChecker : ISpellChecker
{
	private const string IssueId = "UK_UA_SPELLING_ERROR";
	private const string UnknownWordIssueId = "UK_UA_SPELLING_UNKNOWN_WORD";
	private const char CanonicalApostrophe = '\'';

	private const int MinCandidatesForEarlyExit = 3;

	private readonly FuzzyMatcher _fuzzyMatcher;
	private readonly VirtualSymSpell _virtualSymSpell;
	private readonly LexiconCache _lexicon;
	private readonly NgramLanguageModel _ngramLanguageModel;

	public bool EnableTelemetry { get; set; }
	public SpellCheckTelemetry? LastTelemetry { get; private set; }

	private static readonly Regex SurzhykIrovatyRegex = new("іроват(и|ь)$", RegexOptions.Compiled);
	private static readonly Regex SurzhykShtykRegex = new("щик(а|у|ом|ів)?$", RegexOptions.Compiled);

	public UkrainianSpellChecker(
		FuzzyMatcher fuzzyMatcher,
		VirtualSymSpell virtualSymSpell,
		LexiconCache lexicon,
		NgramLanguageModel ngramLanguageModel)
	{
		_fuzzyMatcher = fuzzyMatcher;
		_virtualSymSpell = virtualSymSpell;
		_lexicon = lexicon;
		_ngramLanguageModel = ngramLanguageModel;
	}

	public IEnumerable<TextIssue> Check(AnalyzedDocument document)
	{
		var telemetry = EnableTelemetry ? new SpellCheckTelemetry() : null;
		LastTelemetry = telemetry;

		long start = 0;

		if (EnableTelemetry) start = Stopwatch.GetTimestamp();
		var sessionCache = new DocumentSessionCache(document);
		if (EnableTelemetry) telemetry!.SessionCacheInitMs += GetElapsedMs(start);

		var contextRanker = new ContextRanker(sessionCache, _ngramLanguageModel);

		foreach (var sentence in document.Sentences)
		{
			foreach (var token in sentence.Tokens)
			{
				if (token.Type != TokenType.Word || !token.IsUnknown()) continue;

				if (EnableTelemetry) start = Stopwatch.GetTimestamp();
				string normalizedText = NormalizeApostrophes(token.NormalizedText);
				bool hasHomoglyphs = HasLatinHomoglyphs(token.Text);
				if (EnableTelemetry) telemetry!.StringNormalizationMs += GetElapsedMs(start);

				if (!hasHomoglyphs && ContainsNonUkrainianChars(normalizedText)) continue;

				var combinedCandidates = new List<CorrectionCandidate>();
				string searchTarget = hasHomoglyphs ? CleanHomoglyphs(normalizedText) : normalizedText;
				char userApostropheStyle = DetectUserApostrophe(token.Text);

				if (EnableTelemetry) start = Stopwatch.GetTimestamp();

				if (hasHomoglyphs)
				{
					TryAddDictCandidates(searchTarget, combinedCandidates, 0f, 2.0f);
				}

				if (combinedCandidates.Count == 0 && searchTarget.Length >= 4)
				{
					foreach (var splitCandidate in _virtualSymSpell.GetSplitCandidates(searchTarget))
					{
						combinedCandidates.Add(splitCandidate);
					}
				}

				foreach (var heuristicWord in GenerateLinguisticHeuristics(searchTarget))
				{
					TryAddDictCandidates(heuristicWord, combinedCandidates, distance: 0.5f, bonus: 5.0f);
				}
				if (EnableTelemetry) telemetry!.HeuristicsGenerationMs += GetElapsedMs(start);

				if (combinedCandidates.Count < MinCandidatesForEarlyExit)
				{
					if (EnableTelemetry) start = Stopwatch.GetTimestamp();

					foreach (var ed1 in _virtualSymSpell.GetCandidates(searchTarget))
					{
						if (!combinedCandidates.Any(c => c.Word == ed1.Word))
							combinedCandidates.Add(ed1);
					}

					if (EnableTelemetry) telemetry!.SymSpellMs += GetElapsedMs(start);
				}

				//if (combinedCandidates.Count == 0)
				//{
				//	if (EnableTelemetry) start = Stopwatch.GetTimestamp();

				//	foreach (var fuzzy in _fuzzyMatcher.Suggest(searchTarget))
				//	{
				//		combinedCandidates.Add(fuzzy);
				//	}

				//	if (EnableTelemetry) telemetry!.FuzzyMatcherMs += GetElapsedMs(start);
				//}

				if (EnableTelemetry) start = Stopwatch.GetTimestamp();
				var rankedCandidates = contextRanker.Rank(sentence, token, combinedCandidates);
				if (EnableTelemetry) telemetry!.ContextRankingMs += GetElapsedMs(start);

				if (EnableTelemetry) start = Stopwatch.GetTimestamp();
				var suggestions = rankedCandidates
					.Take(5)
					.Select(c => MatchCapitalization(RestoreApostropheStyle(c.Word, userApostropheStyle), token.Text))
					.Distinct()
					.ToList();

				if (EnableTelemetry) telemetry!.SuggestionFormattingMs += GetElapsedMs(start);

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

	private void TryAddDictCandidates(string word, List<CorrectionCandidate> pool, float distance, float bonus)
	{
		var dictTags = _lexicon.GetCandidates(word);
		if (dictTags.Count > 0)
		{
			// Artificially low score ensures heuristics beat blind fuzzy matches
			float score = distance - bonus - 10.0f;
			pool.Add(new CorrectionCandidate(word, distance, 31, score, dictTags.Select(tg => tg.Tagset).ToArray()));
		}
	}

	private static IEnumerable<string> GenerateLinguisticHeuristics(string word)
	{
		if (word.Length < 3) yield break;

		// 1. Assimilation & Consonant groups
		if (word.Contains("стн")) yield return word.Replace("стн", "сн");
		if (word.Contains("сн")) yield return word.Replace("сн", "стн");
		if (word.Contains("ждн")) yield return word.Replace("ждн", "жн");
		if (word.Contains("здн")) yield return word.Replace("здн", "зн");
		if (word.Contains("тч")) yield return word.Replace("тч", "чч");
		if (word.Contains("шся")) yield return word.Replace("шся", "шся").Replace("шся", "сся");

		// 2. Surzhyk Suffixes
		if (SurzhykIrovatyRegex.IsMatch(word))
			yield return SurzhykIrovatyRegex.Replace(word, "юват$1");

		if (SurzhykShtykRegex.IsMatch(word))
			yield return SurzhykShtykRegex.Replace(word, "ник$1");

		// 3. Rule of Nine (Foreign words i/y confusion)
		if (word.Contains('і'))
		{
			yield return word.Replace("ді", "ди").Replace("ті", "ти").Replace("зі", "зи")
							 .Replace("сі", "си").Replace("ці", "ци").Replace("чі", "чи")
							 .Replace("ші", "ши").Replace("жі", "жи").Replace("рі", "ри");
		}
		if (word.Contains('и'))
		{
			yield return word.Replace("ди", "ді").Replace("ти", "ті").Replace("зи", "зі")
							 .Replace("си", "сі").Replace("ци", "ці").Replace("чи", "чі")
							 .Replace("ши", "ші").Replace("жи", "жі").Replace("ри", "рі");
		}

		// 4. Prefixes (З/С Rule)
		if (word.StartsWith("с") && !Regex.IsMatch(word, "^с[кптфх]")) yield return "з" + word[1..];
		if (word.StartsWith("з") && Regex.IsMatch(word, "^з[кптфх]")) yield return "с" + word[1..];
		if (word.StartsWith("рос")) yield return "роз" + word[3..];
		if (word.StartsWith("бес")) yield return "без" + word[3..];

		// 5. Verbs and Endings
		if (word.EndsWith("ця")) yield return word[..^2] + "ться";
		if (word.EndsWith("тця")) yield return word[..^3] + "ться";
		if (word.EndsWith("ся")) yield return word[..^2] + "шся";
		if (word.EndsWith("т") || word.EndsWith("ть")) yield return word + "ь";

		// 6. Noun Endings (Surzhyk / Russian influence)
		if (word.EndsWith("ова")) yield return word[..^3] + "ого";
		if (word.EndsWith("ева")) yield return word[..^3] + "его";
		if (word.EndsWith("ьом")) yield return word[..^3] + "ем";
		if (word.EndsWith("цьом")) yield return word[..^4] + "цем";

		// 7. Missing Gemination
		if (word.EndsWith("ня")) yield return word[..^2] + "ння";
		if (word.EndsWith("тя")) yield return word[..^2] + "ття";
		if (word.EndsWith("ля")) yield return word[..^2] + "лля";

		// 8. Apostrophe anomalies
		for (int i = 0; i < word.Length - 1; i++)
		{
			if (IsLabialOrPrefix(word[i]) && IsIotated(word[i + 1]))
				yield return word[..(i + 1)] + "'" + word[(i + 1)..];
		}
	}

	private static bool IsLabialOrPrefix(char c) => c is 'б' or 'п' or 'в' or 'м' or 'ф' or 'р' or 'д' or 'з' or 'с';
	private static bool IsIotated(char c) => c is 'я' or 'ю' or 'є' or 'ї';

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

	private static double GetElapsedMs(long startTimestamp) => (Stopwatch.GetTimestamp() - startTimestamp) * 1000.0 / Stopwatch.Frequency;
}