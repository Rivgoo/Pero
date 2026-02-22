using Pero.Abstractions.Contracts;
using Pero.Abstractions.Models;
using Pero.Languages.Uk_UA.Components.Caching;
using Pero.Languages.Uk_UA.Components.Disambiguation;

namespace Pero.Languages.Uk_UA.Components;

/// <summary>
/// Orchestrates morphological analysis for Ukrainian.
/// Performs dictionary lookups, applies context disambiguation rules, and selects the final form.
/// </summary>
public class UkrainianMorphologyAnalyzer : IMorphologyAnalyzer
{
	private readonly LexiconCache _lexicon;
	private readonly IReadOnlyList<IDisambiguationRule> _rules;

	public UkrainianMorphologyAnalyzer(LexiconCache lexicon, IEnumerable<IDisambiguationRule> rules)
	{
		_lexicon = lexicon;
		_rules = rules.ToList();
	}

	public void Enrich(Sentence sentence)
	{
		// Phase 1: Populate all raw candidates from the dictionary
		foreach (var token in sentence.Tokens)
		{
			if (token.Type == TokenType.Word && token.MorphologicalCandidates == null)
			{
				token.MorphologicalCandidates = _lexicon.GetCandidates(token.NormalizedText);
			}
		}

		// Phase 2: Apply disambiguation rules based on sentence context
		foreach (var token in sentence.Tokens)
		{
			if (token.Type != TokenType.Word || token.MorphologicalCandidates == null || token.MorphologicalCandidates.Count == 0)
			{
				continue;
			}

			var candidates = token.MorphologicalCandidates;

			// Run through all disambiguation constraints
			foreach (var rule in _rules)
			{
				if (candidates.Count == 1) break;
				candidates = rule.Apply(sentence, token, candidates);
			}

			// Phase 3: Resolution
			// If rules successfully isolated one candidate, or we must fallback to the first/most frequent one.
			// Currently, we pick the first remaining candidate as the baseline fallback.
			token.Morph = candidates[0];

			// Optional: Update candidates list to reflect the narrowed down choices
			token.MorphologicalCandidates = candidates;
		}
	}
}