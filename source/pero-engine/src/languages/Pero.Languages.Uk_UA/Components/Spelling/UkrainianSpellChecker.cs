using Pero.Abstractions.Contracts;
using Pero.Abstractions.Models;
using Pero.Kernel.Utils;
using Pero.Languages.Uk_UA.Components.Caching;
using Pero.Languages.Uk_UA.Components.Spelling.Context;
using Pero.Languages.Uk_UA.Dictionaries.Fuzzy;
using Pero.Languages.Uk_UA.Dictionaries.Ngrams;

namespace Pero.Languages.Uk_UA.Components;

public partial class UkrainianSpellChecker : ISpellChecker
{
	private const string IssueId = "UK_UA_SPELLING_ERROR";
	private const char CanonicalApostrophe = '\'';

	private readonly FuzzyMatcher _fuzzyMatcher;
	private readonly VirtualSymSpell _virtualSymSpell;
	private readonly LexiconCache _lexicon;
	private readonly NgramLanguageModel _ngramLanguageModel;
	private readonly MorphologicalFilter _morphologicalFilter;

	public UkrainianSpellChecker(
		FuzzyMatcher fuzzyMatcher,
		VirtualSymSpell virtualSymSpell,
		LexiconCache lexicon,
		NgramLanguageModel ngramLanguageModel,
		MorphologicalFilter morphologicalFilter)
	{
		_fuzzyMatcher = fuzzyMatcher;
		_virtualSymSpell = virtualSymSpell;
		_lexicon = lexicon;
		_ngramLanguageModel = ngramLanguageModel;
		_morphologicalFilter = morphologicalFilter;
	}

	public IEnumerable<TextIssue> Check(AnalyzedDocument document)
	{
		var sessionCache = new DocumentSessionCache(document);
		var contextRanker = new ContextRanker(sessionCache, _ngramLanguageModel);

		foreach (var sentence in document.Sentences)
		{
			foreach (var token in sentence.Tokens)
			{
				if (IsTechnical(token)) continue;

				if (token.IsUnknown())
				{
					if (ContainsNonUkrainianChars(token.Text)) continue;

					string searchTarget = CleanStrayPunctuation(NormalizeApostrophes(token.NormalizedText));
					char userApostropheStyle = DetectUserApostrophe(token.Text);

					var combinedCandidates = new List<CorrectionCandidate>();

					foreach (var heuristicWord in GenerateHeuristics(searchTarget))
					{
						var dictTags = _lexicon.GetCandidates(heuristicWord);
						if (dictTags.Count > 0)
						{
							combinedCandidates.Add(new CorrectionCandidate(
								heuristicWord, 0f, 31, -15.0f, dictTags.Select(t => t.Tagset).ToArray()));
						}
					}

					if (combinedCandidates.Count < 3)
					{
						var ed1Results = _virtualSymSpell.GetCandidates(searchTarget);
						foreach (var ed1 in ed1Results)
						{
							if (!combinedCandidates.Any(c => c.Word == ed1.Word))
								combinedCandidates.Add(ed1);
						}
					}

					if (combinedCandidates.Count == 0 || !combinedCandidates.Any(c => c.Score < -1.0f))
					{
						var fuzzyResults = _fuzzyMatcher.Suggest(searchTarget);
						foreach (var fuzzy in fuzzyResults)
						{
							if (!combinedCandidates.Any(c => c.Word == fuzzy.Word))
								combinedCandidates.Add(fuzzy);
						}
					}

					if (combinedCandidates.Count == 0) continue;

					//var profile = _morphologicalFilter.BuildProfile(sentence, token);
					//var morphologicallyFiltered = _morphologicalFilter.ExpandAndFilter(combinedCandidates, profile);

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

	private static IEnumerable<string> GenerateHeuristics(string word)
	{
		yield return word;

		if (word.Length < 3) yield break;

		if (word.EndsWith("ця")) yield return word[..^2] + "ться";
		if (word.EndsWith("тця")) yield return word[..^3] + "ться";
		if (word.EndsWith("ся")) yield return word[..^2] + "шся";

		if (word.EndsWith("т") || word.EndsWith("ть")) yield return word + "ь";
		if (word.EndsWith("ш")) yield return word + "ь";

		if (word.EndsWith("ттю")) yield return word[..^3] + "тю";
		if (word.EndsWith("лю") && !word.EndsWith("ллю")) yield return word[..^2] + "ллю";
		if (word.EndsWith("ню") && !word.EndsWith("нню")) yield return word[..^2] + "нню";
		if (word.EndsWith("чю") && !word.EndsWith("ччю")) yield return word[..^2] + "ччю";
		if (word.EndsWith("шю") && !word.EndsWith("шшю")) yield return word[..^2] + "шшю";
		if (word.EndsWith("цю") && !word.EndsWith("ццю")) yield return word[..^2] + "ццю";
		if (word.EndsWith("жю") && !word.EndsWith("жжю")) yield return word[..^2] + "жжю";
		if (word.EndsWith("міцю")) yield return word[..^4] + "міццю";

		if (word.EndsWith("шь") || word.EndsWith("чь") || word.EndsWith("щь") || word.EndsWith("жь")) yield return word[..^1];

		if (word.EndsWith("ова")) yield return word[..^3] + "ого";
		if (word.EndsWith("ева")) yield return word[..^3] + "его";

		if (word.EndsWith("ня")) yield return word[..^2] + "ння";
		if (word.EndsWith("тя")) yield return word[..^2] + "ття";
		if (word.EndsWith("ля")) yield return word[..^2] + "лля";

		if (word.EndsWith("ьом")) yield return word[..^3] + "ем";
		if (word.EndsWith("цьом")) yield return word[..^4] + "цем";

		if (word.Contains("ько") && !word.Contains("тько") && !word.Contains("дько"))
			yield return word.Replace("ько", "тько");

		for (int i = 0; i < word.Length - 1; i++)
		{
			if (IsLabialOrPrefix(word, i) && IsIotated(word[i + 1]))
				yield return word.Substring(0, i + 1) + "'" + word.Substring(i + 1);
		}
	}

	private static bool IsLabialOrPrefix(string word, int index)
	{
		char c = word[index];
		if (c is 'б' or 'п' or 'в' or 'м' or 'ф' or 'р' or 'д') return true;
		if (c == 'з' || c == 'с') return true;
		return false;
	}

	private static bool IsIotated(char c) => c is 'я' or 'ю' or 'є' or 'ї';

	private static bool ContainsNonUkrainianChars(string word)
	{
		foreach (char c in word)
		{
			if (!IsUkrainianChar(c)) return true;
		}
		return false;
	}

	private static bool IsUkrainianChar(char c)
	{
		if (c >= 'а' && c <= 'щ') return true;
		if (c >= 'ю' && c <= 'я') return true;
		if (c == 'ь') return true;

		if (c >= 'А' && c <= 'Щ') return true;
		if (c >= 'Ю' && c <= 'Я') return true;
		if (c == 'Ь') return true;

		if (c == 'і' || c == 'І') return true;
		if (c == 'ї' || c == 'Ї') return true;
		if (c == 'є' || c == 'Є') return true;
		if (c == 'ґ' || c == 'Ґ') return true;

		if (c == '\'' || c == '’' || c == 'ʼ') return true;
		if (c == '-') return true;

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

	private static string CleanStrayPunctuation(string word)
	{
		if (word.Contains(',') || word.Contains('.'))
		{
			return word.Replace(",", "").Replace(".", "");
		}

		if (word.Contains("''"))
		{
			return word.Replace("''", "'");
		}

		return word;
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