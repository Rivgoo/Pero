using Pero.Abstractions.Models;

namespace Pero.Abstractions.Contracts;

/// <summary>
/// Defines the contract for scanning text to identify and separate
/// natural language from technical entities like URLs or code.
/// </summary>
public interface IPreTokenizer
{
	/// <summary>
	/// Scans the text and partitions it into a sequence of fragments.
	/// </summary>
	/// <param name="cleanedText">The normalized text from the ITextCleaner.</param>
	/// <returns>An enumeration of text fragments.</returns>
	IEnumerable<TextFragment> Scan(string cleanedText);
}