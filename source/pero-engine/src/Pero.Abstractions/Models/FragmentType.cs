namespace Pero.Abstractions.Models;

/// <summary>
/// Defines the type of a text fragment identified by the pre-tokenizer.
/// </summary>
public enum FragmentType
{
	/// <summary>
	/// A fragment of natural language text that requires further tokenization.
	/// </summary>
	Raw,
	/// <summary>
	/// A URL.
	/// </summary>
	Url,
	/// <summary>
	/// An email address.
	/// </summary>
	Email,
	/// <summary>
	/// A snippet of code, typically enclosed in backticks.
	/// </summary>
	CodeSnippet,
	/// <summary>
	/// A social media mention or hashtag.
	/// </summary>
	Mention
}