using Pero.Abstractions.Models;

namespace Pero.Abstractions.Contracts;

/// <summary>
/// Defines the contract for enriching tokens with linguistic annotations.
/// </summary>
public interface IMorphologyAnalyzer
{
	/// <summary>
	/// Analyzes the tokens within a sentence and assigns morphological information.
	/// This method typically mutates the 'Morph' property of each token.
	/// </summary>
	/// <param name="sentence">The sentence whose tokens need enrichment.</param>
	void Enrich(Sentence sentence);
}