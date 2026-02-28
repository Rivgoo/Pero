using Pero.Abstractions.Models;
using Pero.Abstractions.Models.Morphology;

namespace Pero.Languages.Uk_UA.Components.Disambiguation;

/// <summary>
/// Defines a contract for context-aware disambiguation rules.
/// </summary>
public interface IDisambiguationRule
{
	/// <summary>
	/// Evaluates the context of a token and filters out impossible morphological candidates.
	/// </summary>
	/// <param name="sentence">The complete sentence for context.</param>
	/// <param name="token">The current token to disambiguate.</param>
	/// <param name="candidates">The current list of valid candidates.</param>
	/// <returns>A narrowed down list of candidates, or the original list if the rule doesn't apply.</returns>
	IReadOnlyList<MorphologicalInfo> Apply(Sentence sentence, Token token, IReadOnlyList<MorphologicalInfo> candidates);
}