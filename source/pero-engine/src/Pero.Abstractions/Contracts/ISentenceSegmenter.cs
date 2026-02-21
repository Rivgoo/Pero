using Pero.Abstractions.Models;

namespace Pero.Abstractions.Contracts;

/// <summary>
/// Defines the contract for grouping a flat stream of tokens into sentences.
/// </summary>
public interface ISentenceSegmenter
{
	/// <summary>
	/// Groups a sequence of tokens into sentences.
	/// </summary>
	/// <param name="tokens">The flat list of tokens for the entire document.</param>
	/// <returns>An enumeration of sentences.</returns>
	IEnumerable<Sentence> Segment(IEnumerable<Token> tokens);
}