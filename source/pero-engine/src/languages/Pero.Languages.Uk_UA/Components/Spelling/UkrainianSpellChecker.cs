using Pero.Abstractions.Contracts;
using Pero.Abstractions.Models;
using Pero.Kernel.Utils;
using Pero.Languages.Uk_UA.Components.Caching;
using Pero.Languages.Uk_UA.Components.Spelling.Context;
using Pero.Languages.Uk_UA.Dictionaries.Fuzzy;

namespace Pero.Languages.Uk_UA.Components;

public partial class UkrainianSpellChecker : ISpellChecker
{
	private const string IssueId = "UK_UA_SPELLING_ERROR";
	private const char CanonicalApostrophe = '\'';
	private readonly FuzzyMatcher _fuzzyMatcher;
	private readonly LexiconCache _lexicon;

	private static readonly Dictionary<char, char> EnToUkLayout = new()
	{
		{'q', 'й'}, {'w', 'ц'}, {'e', 'у'}, {'r', 'к'}, {'t', 'е'}, {'y', 'н'}, {'u', 'г'}, {'i', 'ш'}, {'o', 'щ'}, {'p', 'з'}, {'[', 'х'}, {']', 'ї'},
		{'a', 'ф'}, {'s', 'і'}, {'d', 'в'}, {'f', 'а'}, {'g', 'п'}, {'h', 'р'}, {'j', 'о'}, {'k', 'л'}, {'l', 'д'}, {';', 'ж'}, {'\'', 'є'},
		{'z', 'я'}, {'x', 'ч'}, {'c', 'с'}, {'v', 'м'}, {'b', 'и'}, {'n', 'т'}, {'m', 'ь'}, {',', 'б'}, {'.', 'ю'}
	};

	public UkrainianSpellChecker(FuzzyMatcher fuzzyMatcher, LexiconCache lexicon)
	{
		_fuzzyMatcher = fuzzyMatcher;
		_lexicon = lexicon;
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
					if (ShouldSkipSpellcheck(token.Text, out bool isPureLatin)) continue;

					string searchTarget = NormalizeApostrophes(token.NormalizedText);
					char userApostropheStyle = DetectUserApostrophe(token.Text);

					if (isPureLatin) searchTarget = TranslateLayout(searchTarget);

					// Hybrid Search: Heuristics + Fuzzy
					var combinedCandidates = new List<CorrectionCandidate>();

					// 1. O(1) Pre-flight Heuristics (Distance 0.0 or low cost)
					foreach (var heuristicWord in GenerateHeuristics(searchTarget))
					{
						var dictTags = _lexicon.GetCandidates(heuristicWord);
						if (dictTags.Count > 0)
						{
							// Give massive bonus score to rule-based matches
							combinedCandidates.Add(new CorrectionCandidate(
								heuristicWord, 0f, 31, -5.0f, dictTags.Select(t => t.Tagset).ToArray()));
						}
					}

					// 2. FST Fuzzy Search
					var fuzzyResults = _fuzzyMatcher.Suggest(searchTarget);
					foreach (var fuzzy in fuzzyResults)
					{
						if (!combinedCandidates.Any(c => c.Word == fuzzy.Word))
							combinedCandidates.Add(fuzzy);
					}

					if (combinedCandidates.Count == 0) continue;

					// 3. Context Ranking
					var rankedCandidates = contextRanker.Rank(sentence, token, combinedCandidates);

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

	/// <summary>
	/// Generates candidates based on common Ukrainian grammatical and phonetic error patterns.
	/// </summary>
	private static IEnumerable<string> GenerateHeuristics(string word)
	{
		if (word.Length < 3) yield break;

		// Verb endings
		if (word.EndsWith("ця")) yield return word[..^2] + "ться";
		if (word.EndsWith("тця")) yield return word[..^3] + "ться";
		if (word.EndsWith("ся")) yield return word[..^2] + "шся"; // дивися -> дивишся

		// Instrumental case doublings
		if (word.EndsWith("ттю")) yield return word[..^3] + "тю"; // радісттю -> радістю
		if (word.EndsWith("лю") && !word.EndsWith("ллю")) yield return word[..^2] + "ллю";
		if (word.EndsWith("ню") && !word.EndsWith("нню")) yield return word[..^2] + "нню";
		if (word.EndsWith("чю") && !word.EndsWith("ччю")) yield return word[..^2] + "ччю";
		if (word.EndsWith("шю") && !word.EndsWith("шшю")) yield return word[..^2] + "шшю";
		if (word.EndsWith("цю") && !word.EndsWith("ццю")) yield return word[..^2] + "ццю";
		if (word.EndsWith("жю") && !word.EndsWith("жжю")) yield return word[..^2] + "жжю";
		if (word.EndsWith("міцю")) yield return word[..^4] + "міццю";

		// Soft sign russianisms
		if (word.EndsWith("шь") || word.EndsWith("чь") || word.EndsWith("щь") || word.EndsWith("жь"))
			yield return word[..^1];

		// Adjective endings (russianisms)
		if (word.EndsWith("ова")) yield return word[..^3] + "ого";
		if (word.EndsWith("ева")) yield return word[..^3] + "его";

		// Double consonants inside word (missing)
		if (word.Contains("н") && !word.Contains("нн")) yield return word.Replace("н", "нн"); // not precise, but fast check

		// Common Doubling Fixes (targeted)
		if (word.EndsWith("ня")) yield return word[..^2] + "ння";
		if (word.EndsWith("тя")) yield return word[..^2] + "ття";
		if (word.EndsWith("ля")) yield return word[..^2] + "лля";

		// Instrumental O-E
		if (word.EndsWith("ьом")) yield return word[..^3] + "ем";
		if (word.EndsWith("цьом")) yield return word[..^4] + "цем";

		// Apostrophe injection
		for (int i = 0; i < word.Length - 1; i++)
		{
			if (IsLabialOrR(word[i]) && IsIotated(word[i + 1]))
				yield return word.Substring(0, i + 1) + "'" + word.Substring(i + 1);
		}
	}

	private static bool IsLabialOrR(char c) => c is 'б' or 'п' or 'в' or 'м' or 'ф' or 'р';
	private static bool IsIotated(char c) => c is 'я' or 'ю' or 'є' or 'ї';

	private static bool ShouldSkipSpellcheck(string word, out bool isPureLatin)
	{
		isPureLatin = true;
		if (string.IsNullOrWhiteSpace(word)) return true;
		bool hasCyrillic = false;
		foreach (char c in word)
		{
			if (char.IsDigit(c)) return true;
			if ((c >= '\u0400' && c <= '\u04FF') || (c >= '\u0500' && c <= '\u052F'))
			{
				hasCyrillic = true;
				isPureLatin = false;
			}
		}
		if (hasCyrillic && isPureLatin == false && word.Any(c => c >= 'a' && c <= 'z')) return true;
		return false;
	}

	private static string TranslateLayout(string latinWord)
	{
		var chars = latinWord.ToCharArray();
		for (int i = 0; i < chars.Length; i++)
		{
			if (EnToUkLayout.TryGetValue(chars[i], out char ukChar)) chars[i] = ukChar;
		}
		return new string(chars);
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

	private static bool IsTechnical(Token token) => token.Type != TokenType.Word;

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