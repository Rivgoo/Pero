using Pero.Abstractions.Models;
using Pero.Abstractions.Models.Morphology;
using Pero.Kernel.Utils;
using Pero.Languages.Uk_UA.Dictionaries.Fuzzy;
using Pero.Languages.Uk_UA.Dictionaries.Ngrams;
using Pero.Languages.Uk_UA.Utils;
using System.Text;

namespace Pero.Languages.Uk_UA.Components.Spelling.Context;

public class ContextRanker
{
	private readonly DocumentSessionCache _sessionCache;
	private readonly NgramLanguageModel _ngramModel;

	private const float NgramBonusMultiplier = 0.015f;

	public ContextRanker(DocumentSessionCache sessionCache, NgramLanguageModel ngramModel)
	{
		_sessionCache = sessionCache;
		_ngramModel = ngramModel;
	}

	public IReadOnlyList<CorrectionCandidate> Rank(
		Sentence sentence,
		Token errorToken,
		IReadOnlyList<CorrectionCandidate> candidates)
	{
		var prevToken = sentence.GetPreviousSignificantToken(errorToken);
		var nextToken = sentence.GetNextSignificantToken(errorToken);

		ulong prevHash = prevToken != null && prevToken.Type == TokenType.Word ? MurmurHash3.Hash(prevToken.NormalizedText) : 0;
		ulong nextHash = nextToken != null && nextToken.Type == TokenType.Word ? MurmurHash3.Hash(nextToken.NormalizedText) : 0;

		var rankedList = new List<CorrectionCandidate>(candidates.Count);

		foreach (var candidate in candidates)
		{
			float penalty = 0f;
			float bonus = 0f;

			// 1. Morphological Agreement Rules
			if (IsPreposition(prevToken, out string prepText))
				penalty += EvaluatePrepositionGovernment(prepText, candidate.Tagsets);

			if (IsAdjective(prevToken, out var adjTags))
				penalty += EvaluateAgreement(adjTags, candidate.Tagsets, PartOfSpeech.Noun);
			else if (IsNoun(nextToken, out var nounTags))
				penalty += EvaluateAgreement(nounTags, candidate.Tagsets, PartOfSpeech.Adjective);

			// 2. Document Session Cache (Word used elsewhere in text)
			bonus += _sessionCache.GetSessionBonus(candidate.Word);

			// 3. N-gram Contextual Ranking
			ulong candidateHash = MurmurHash3.Hash(candidate.Word);

			if (prevHash != 0)
			{
				byte score = _ngramModel.GetBigramScore(prevHash, candidateHash);
				bonus += score * NgramBonusMultiplier;

				if (nextHash != 0)
				{
					byte triScore = _ngramModel.GetTrigramScore(prevHash, candidateHash, nextHash);
					bonus += triScore * (NgramBonusMultiplier * 1.5f); // Trigrams are very strong signals
				}
			}

			if (nextHash != 0)
			{
				byte score = _ngramModel.GetBigramScore(candidateHash, nextHash);
				bonus += score * NgramBonusMultiplier;
			}

			// Heuristic flag bonus
			if (candidate.Score < -1.0f) bonus += 2.0f;

			float adjustedScore = candidate.Score + penalty - bonus;
			if (adjustedScore < -10.0f) adjustedScore = -10.0f;

			rankedList.Add(new CorrectionCandidate(
				candidate.Word,
				candidate.Distance,
				candidate.Frequency,
				adjustedScore,
				candidate.Tagsets));
		}

		rankedList.Sort();
		return rankedList;
	}

	private static bool IsPreposition(Token? token, out string text)
	{
		text = string.Empty;
		if (token?.Morph?.Tagset.PartOfSpeech == PartOfSpeech.Preposition)
		{
			text = token.NormalizedText;
			return true;
		}
		return false;
	}

	private static bool IsAdjective(Token? token, out MorphologyTagset tags)
	{
		tags = default;
		if (token?.Morph?.Tagset.PartOfSpeech == PartOfSpeech.Adjective)
		{
			tags = token.Morph.Tagset;
			return true;
		}
		return false;
	}

	private static bool IsNoun(Token? token, out MorphologyTagset tags)
	{
		tags = default;
		if (token?.Morph?.Tagset.PartOfSpeech == PartOfSpeech.Noun)
		{
			tags = token.Morph.Tagset;
			return true;
		}
		return false;
	}

	private static float EvaluatePrepositionGovernment(string preposition, MorphologyTagset[] candidateTags)
	{
		var allowedCases = preposition switch
		{
			"до" or "від" or "з" or "із" or "без" or "для" or "біля" or "після" => new[] { GrammarCase.Genitive },
			"над" or "під" or "перед" or "за" => new[] { GrammarCase.Instrumental, GrammarCase.Accusative },
			"на" or "об" => new[] { GrammarCase.Locative, GrammarCase.Accusative },
			"в" or "у" => new[] { GrammarCase.Locative, GrammarCase.Accusative, GrammarCase.Genitive },
			"по" => new[] { GrammarCase.Locative, GrammarCase.Accusative, GrammarCase.Dative },
			_ => Array.Empty<GrammarCase>()
		};
		if (allowedCases.Length == 0) return 0f;
		bool hasValidCase = candidateTags.Any(tag => allowedCases.Contains(tag.Case));
		return hasValidCase ? 0f : 1.0f; // Soft penalty, not absolute removal
	}

	private static float EvaluateAgreement(MorphologyTagset anchorTag, MorphologyTagset[] candidateTags, PartOfSpeech targetPos)
	{
		bool isTargetPosPresent = candidateTags.Any(t => t.PartOfSpeech == targetPos);
		if (!isTargetPosPresent) return 0f;

		bool hasAgreement = candidateTags.Any(t =>
			t.PartOfSpeech == targetPos &&
			t.Case == anchorTag.Case &&
			(t.Number == GrammarNumber.Plural || t.Gender == anchorTag.Gender) &&
			t.Number == anchorTag.Number);
		return hasAgreement ? 0f : 0.8f;
	}
}