using Pero.Abstractions.Models;
using Pero.Abstractions.Models.Morphology;
using Pero.Kernel.Utils;
using Pero.Languages.Uk_UA.Dictionaries.Fuzzy;

namespace Pero.Languages.Uk_UA.Components.Spelling.Context;

public  class ContextRanker
{
	private readonly DocumentSessionCache _sessionCache;

	public ContextRanker(DocumentSessionCache sessionCache)
	{
		_sessionCache = sessionCache;
	}

	/// <summary>
	/// Evaluates all candidates and returns a re-sorted list prioritizing contextually valid words.
	/// </summary>
	public IReadOnlyList<CorrectionCandidate> Rank(
		Sentence sentence,
		Token errorToken,
		IReadOnlyList<CorrectionCandidate> candidates)
	{
		var prevToken = sentence.GetPreviousSignificantToken(errorToken);
		var nextToken = sentence.GetNextSignificantToken(errorToken);

		var rankedList = new List<CorrectionCandidate>(candidates.Count);

		foreach (var candidate in candidates)
		{
			float penalty = 0f;
			float bonus = 0f;

			if (IsPreposition(prevToken, out string prepText))
			{
				penalty += EvaluatePrepositionGovernment(prepText, candidate.Tagsets);
			}

			if (IsAdjective(prevToken, out var adjTags))
			{
				penalty += EvaluateAgreement(adjTags, candidate.Tagsets, PartOfSpeech.Noun);
			}
			else if (IsNoun(nextToken, out var nounTags))
			{
				penalty += EvaluateAgreement(nounTags, candidate.Tagsets, PartOfSpeech.Adjective);
			}

			bonus += _sessionCache.GetSessionBonus(candidate.Word);

			float adjustedDistance = candidate.Distance + penalty - bonus;

			if (adjustedDistance < 0f) adjustedDistance = 0f;

			rankedList.Add(new CorrectionCandidate(
				candidate.Word,
				adjustedDistance,
				candidate.Frequency,
				candidate.Tagsets));
		}

		// Resort based on the new, context-adjusted distances
		rankedList.Sort();
		return rankedList;
	}

	private static bool IsPreposition(Token? token, out string text)
	{
		text = string.Empty;
		if (token == null) return false;

		if (token.Morph?.Tagset.PartOfSpeech == PartOfSpeech.Preposition)
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

	/// <summary>
	/// Penalizes candidates that do not possess the GrammarCase required by the preceding preposition.
	/// </summary>
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

		// Check if any of the candidate's homonyms match the allowed cases
		bool hasValidCase = candidateTags.Any(tag => allowedCases.Contains(tag.Case));

		// If the word doesn't fit the preposition at all, add a massive penalty.
		return hasValidCase ? 0f : 2.5f;
	}

	/// <summary>
	/// Penalizes candidates if they are the target Part of Speech but fail to agree in Case/Gender/Number.
	/// </summary>
	private static float EvaluateAgreement(MorphologyTagset anchorTag, MorphologyTagset[] candidateTags, PartOfSpeech targetPos)
	{
		bool isTargetPosPresent = candidateTags.Any(t => t.PartOfSpeech == targetPos);
		if (!isTargetPosPresent) return 0f; // Candidate is a completely different POS, let frequency decide

		bool hasAgreement = candidateTags.Any(t =>
			t.PartOfSpeech == targetPos &&
			t.Case == anchorTag.Case &&
			(t.Number == GrammarNumber.Plural || t.Gender == anchorTag.Gender) && // Gender is neutral in plural
			t.Number == anchorTag.Number);

		// Penalize heavily for broken agreement (e.g., "зелена" + "стіл")
		return hasAgreement ? 0f : 1.5f;
	}
}