using Pero.Abstractions.Models;
using Pero.Kernel.Fuzzy;
using Pero.Kernel.Ngrams;
using Pero.Kernel.Utils;

namespace Pero.Languages.Uk_UA.Components.Spelling.Context;

public class ContextRanker
{
	private readonly DocumentSessionCache _sessionCache;
	private readonly NgramLanguageModel _ngramModel;

	private const float NgramBonusMultiplier = 0.01f;

	public ContextRanker(DocumentSessionCache sessionCache, NgramLanguageModel ngramModel)
	{
		_sessionCache = sessionCache;
		_ngramModel = ngramModel;
	}

	public IReadOnlyList<CorrectionCandidate<UkMorphologyTag>> Rank(
		Sentence sentence,
		Token errorToken,
		IReadOnlyList<CorrectionCandidate<UkMorphologyTag>> candidates)
	{
		var prevToken = sentence.GetPreviousSignificantToken(errorToken);
		var nextToken = sentence.GetNextSignificantToken(errorToken);

		ulong prevHash = prevToken != null && prevToken.Type == TokenType.Word ? MurmurHash3.Hash(prevToken.NormalizedText) : 0;
		ulong nextHash = nextToken != null && nextToken.Type == TokenType.Word ? MurmurHash3.Hash(nextToken.NormalizedText) : 0;

		var rankedList = new List<CorrectionCandidate<UkMorphologyTag>>(candidates.Count);

		foreach (var candidate in candidates)
		{
			float bonus = 0f;

			bonus += _sessionCache.GetSessionBonus(candidate.Word);

			ulong candidateHash = MurmurHash3.Hash(candidate.Word);

			if (prevHash != 0)
			{
				byte score = _ngramModel.GetBigramScore(prevHash, candidateHash);
				bonus += score * NgramBonusMultiplier;

				if (nextHash != 0)
				{
					byte triScore = _ngramModel.GetTrigramScore(prevHash, candidateHash, nextHash);
					bonus += triScore * (NgramBonusMultiplier * 1.5f);
				}
			}

			if (nextHash != 0)
			{
				byte score = _ngramModel.GetBigramScore(candidateHash, nextHash);
				bonus += score * NgramBonusMultiplier;
			}

			if (candidate.Score < -1.0f) bonus += 2.0f;

			float adjustedScore = candidate.Score - bonus;
			if (adjustedScore < -10.0f) adjustedScore = -10.0f;

			rankedList.Add(new CorrectionCandidate<UkMorphologyTag>(
				candidate.Word,
				candidate.Distance,
				candidate.Frequency,
				adjustedScore,
				candidate.Tagsets));
		}

		rankedList.Sort();
		return rankedList;
	}
}