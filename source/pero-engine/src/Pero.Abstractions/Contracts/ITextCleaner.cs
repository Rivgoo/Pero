namespace Pero.Abstractions.Contracts;

/// <summary>
/// Defines the contract for initial text normalization.
/// This is the first stage in the analysis pipeline.
/// </summary>
public interface ITextCleaner
{
	/// <summary>
	/// Cleans and normalizes the raw input text.
	/// </summary>
	/// <param name="rawText">The original text.</param>
	/// <returns>A normalized string ready for scanning.</returns>
	string Clean(string rawText);
}