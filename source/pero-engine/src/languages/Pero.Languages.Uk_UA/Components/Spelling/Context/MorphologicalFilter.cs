using Pero.Abstractions.Models;
using Pero.Abstractions.Models.Morphology;
using Pero.Kernel.Dictionaries;
using Pero.Kernel.Fuzzy;
using Pero.Kernel.Utils;

namespace Pero.Languages.Uk_UA.Components.Spelling.Context;

public class MorphologicalFilter
{
	private readonly CompiledDictionary _dictionary;

	public MorphologicalFilter(CompiledDictionary dictionary)
	{
		_dictionary = dictionary;
	}

	public GrammarProfile BuildProfile(Sentence sentence, Token errorToken)
	{
		var prevToken = sentence.GetPreviousSignificantToken(errorToken);
		var nextToken = sentence.GetNextSignificantToken(errorToken);

		if (IsPreposition(prevToken, out string prepText))
		{
			var requiredCases = GetRequiredCasesForPreposition(prepText);
			if (requiredCases.Length > 0)
			{
				return new GrammarProfile(expectedCases: requiredCases);
			}
		}

		if (IsNumeral(prevToken, out string numText))
		{
			var numProfile = GetProfileForNumeral(numText);
			if (!numProfile.IsEmpty) return numProfile;
		}

		if (IsAdjective(prevToken, out var adjTags))
		{
			return new GrammarProfile(
				expectedPos: PartOfSpeech.Noun,
				expectedCases: new[] { adjTags.Case },
				expectedGender: adjTags.Number == GrammarNumber.Plural ? null : adjTags.Gender,
				expectedNumber: adjTags.Number);
		}

		if (IsNoun(nextToken, out var nounTags))
		{
			return new GrammarProfile(
				expectedPos: PartOfSpeech.Adjective,
				expectedCases: new[] { nounTags.Case },
				expectedGender: nounTags.Number == GrammarNumber.Plural ? null : nounTags.Gender,
				expectedNumber: nounTags.Number);
		}

		if (IsNoun(prevToken, out var subjTags) && subjTags.Case == GrammarCase.Nominative)
		{
			return new GrammarProfile(
				expectedPos: PartOfSpeech.Verb,
				expectedNumber: subjTags.Number,
				expectedGender: subjTags.Number == GrammarNumber.Plural ? null : subjTags.Gender);
		}

		return new GrammarProfile();
	}

	public IReadOnlyList<CorrectionCandidate> ExpandAndFilter(
		IReadOnlyList<CorrectionCandidate> initialCandidates,
		GrammarProfile profile)
	{
		if (profile.IsEmpty || initialCandidates.Count == 0) return initialCandidates;

		var expandedPool = new List<CorrectionCandidate>();
		var processedLemmas = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		foreach (var candidate in initialCandidates)
		{
			var analysisResults = _dictionary.Analyze(candidate.Word).ToList();

			if (analysisResults.Count == 0)
			{
				expandedPool.Add(candidate);
				continue;
			}

			foreach (var info in analysisResults)
			{
				if (!processedLemmas.Add(info.Lemma)) continue;

				var allForms = _dictionary.GetAllForms(info.Lemma);

				foreach (var form in allForms)
				{
					if (profile.ExpectedPos.HasValue && form.Tagset.PartOfSpeech != profile.ExpectedPos.Value)
					{
						continue;
					}

					float baseScore = candidate.Score;
					float bonus = 0f;
					float penalty = 0f;

					if (profile.ExpectedCases != null)
					{
						if (form.Tagset.Case == GrammarCase.None || form.Tagset.Case == GrammarCase.Uninflected)
						{
							penalty += 3.0f;
						}
						else if (profile.ExpectedCases.Contains(form.Tagset.Case)) bonus += 2.0f;
						else penalty += 2.0f;
					}

					if (profile.ExpectedGender.HasValue && form.Tagset.Gender != GrammarGender.None)
					{
						if (form.Tagset.Gender == profile.ExpectedGender.Value) bonus += 1.0f;
						else penalty += 1.0f;
					}

					if (profile.ExpectedNumber.HasValue && form.Tagset.Number != GrammarNumber.None)
					{
						if (form.Tagset.Number == profile.ExpectedNumber.Value) bonus += 1.0f;
						else penalty += 1.0f;
					}

					float finalScore = baseScore - bonus + penalty;

					expandedPool.Add(new CorrectionCandidate(
						form.Form,
						candidate.Distance,
						candidate.Frequency,
						finalScore,
						new[] { form.Tagset }
					));
				}
			}
		}

		expandedPool.Sort();

		var distinctRanked = new List<CorrectionCandidate>();
		var seenForms = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		foreach (var expanded in expandedPool)
		{
			if (seenForms.Add(expanded.Word))
			{
				distinctRanked.Add(expanded);
			}
		}

		return distinctRanked;
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
		if (token?.Morph?.Tagset.PartOfSpeech == PartOfSpeech.Noun || token?.Morph?.Tagset.PartOfSpeech == PartOfSpeech.Pronoun)
		{
			tags = token.Morph.Tagset;
			return true;
		}
		return false;
	}

	private static bool IsNumeral(Token? token, out string text)
	{
		text = string.Empty;
		if (token?.Morph?.Tagset.PartOfSpeech == PartOfSpeech.Numeral)
		{
			text = token.NormalizedText;
			return true;
		}
		return false;
	}

	private static GrammarCase[] GetRequiredCasesForPreposition(string preposition)
	{
		return preposition switch
		{
			"до" or "від" or "з-за" or "із-за" or "з-під" or "без" or "для" or "біля" or "після" or "проти" or "серед" or "навколо" or "щодо" => new[] { GrammarCase.Genitive },
			"крізь" or "через" or "про" => new[] { GrammarCase.Accusative },
			"при" => new[] { GrammarCase.Locative },
			"в" or "у" or "на" or "об" => new[] { GrammarCase.Locative, GrammarCase.Accusative },
			"під" or "над" or "перед" or "поза" => new[] { GrammarCase.Instrumental, GrammarCase.Accusative },
			"з" or "із" or "зі" => new[] { GrammarCase.Genitive, GrammarCase.Instrumental, GrammarCase.Accusative },
			"за" => new[] { GrammarCase.Instrumental, GrammarCase.Accusative, GrammarCase.Genitive },
			"по" => new[] { GrammarCase.Locative, GrammarCase.Accusative, GrammarCase.Dative },
			_ => Array.Empty<GrammarCase>()
		};
	}

	private static GrammarProfile GetProfileForNumeral(string numeral)
	{
		return numeral switch
		{
			"один" or "одна" or "одне" => new GrammarProfile(expectedPos: PartOfSpeech.Noun, expectedCases: new[] { GrammarCase.Nominative }, expectedNumber: GrammarNumber.Singular),
			"два" or "дві" or "три" or "чотири" or "обидва" or "обидві" => new GrammarProfile(expectedPos: PartOfSpeech.Noun, expectedCases: new[] { GrammarCase.Nominative, GrammarCase.Genitive }, expectedNumber: GrammarNumber.Plural),
			"п'ять" or "шість" or "сім" or "вісім" or "дев'ять" or "десять" => new GrammarProfile(expectedPos: PartOfSpeech.Noun, expectedCases: new[] { GrammarCase.Genitive }, expectedNumber: GrammarNumber.Plural),
			_ => new GrammarProfile()
		};
	}
}