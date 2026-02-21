using Pero.Abstractions.Models;

namespace Pero.Abstractions.Contracts;

/// <summary>
/// Defines the contract for splitting a raw text fragment into a sequence of atomic tokens.
/// </summary>
public interface ITokenizer
{
	/// <summary>
	/// Analyzes a raw text fragment and returns a sequence of tokens.
	/// </summary>
	/// <param name="fragment">The text fragment to process.</param>
	/// <returns>An enumeration of tokens.</returns>
	IEnumerable<Token> Tokenize(TextFragment fragment);
}