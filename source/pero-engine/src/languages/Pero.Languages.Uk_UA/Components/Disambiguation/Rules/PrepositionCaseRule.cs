using Pero.Abstractions.Models;
using Pero.Abstractions.Models.Morphology;
using Pero.Kernel.Utils;
using Pero.Languages.Uk_UA.Models.Morphology;

namespace Pero.Languages.Uk_UA.Components.Disambiguation.Rules;

/// <summary>
/// Filters candidates based on the grammatical case required by the preceding preposition.
/// Adjectives and nouns following a preposition must match its required case.
/// </summary>
public class PrepositionCaseRule : IDisambiguationRule
{
	public IReadOnlyList<MorphologicalInfo> Apply(Sentence sentence, Token token, IReadOnlyList<MorphologicalInfo> candidates)
	{
		bool isApplicablePart = candidates.Any(c =>
			c.Tag is UkMorphologyTag tag &&
			(tag.PartOfSpeech == PartOfSpeech.Noun ||
			 tag.PartOfSpeech == PartOfSpeech.Adjective ||
			 tag.PartOfSpeech == PartOfSpeech.Pronoun));

		if (!isApplicablePart) return candidates;

		var prevToken = FindPrecedingPreposition(sentence, token);
		if (prevToken == null) return candidates;

		var allowedCases = GetAllowedCases(prevToken.NormalizedText);
		if (allowedCases.Length == 0) return candidates;

		var filtered = candidates.Where(c => c.Tag is UkMorphologyTag tag && allowedCases.Contains(tag.Case)).ToList();

		return filtered.Count > 0 ? filtered : candidates;
	}

	private static Token? FindPrecedingPreposition(Sentence sentence, Token current)
	{
		var prev = sentence.GetPreviousSignificantToken(current);

		if (prev != null && prev.MorphologicalCandidates?.Any(c => c.Tag is UkMorphologyTag tag && tag.PartOfSpeech == PartOfSpeech.Adjective) == true)
		{
			var prevPrev = sentence.GetPreviousSignificantToken(prev);
			if (IsPreposition(prevPrev)) return prevPrev;
		}

		return IsPreposition(prev) ? prev : null;
	}

	private static bool IsPreposition(Token? token)
	{
		return token != null && token.MorphologicalCandidates?.Any(c => c.Tag is UkMorphologyTag tag && tag.PartOfSpeech == PartOfSpeech.Preposition) == true;
	}

	private static GrammarCase[] GetAllowedCases(string preposition) => preposition switch
	{
		"до" or "від" or "з" or "із" or "без" or "для" or "біля" or "після" => new[] { GrammarCase.Genitive },
		"над" or "під" or "перед" or "за" => new[] { GrammarCase.Instrumental, GrammarCase.Accusative },
		"на" or "об" => new[] { GrammarCase.Locative, GrammarCase.Accusative },
		"в" or "у" => new[] { GrammarCase.Locative, GrammarCase.Accusative, GrammarCase.Genitive },
		"по" => new[] { GrammarCase.Locative, GrammarCase.Accusative, GrammarCase.Dative },
		_ => Array.Empty<GrammarCase>()
	};
}