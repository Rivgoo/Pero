namespace Pero.Abstractions.Models;

/// <summary>
/// An atomic unit of text, such as a word, punctuation mark, or a technical entity.
/// This is the central data model for analysis.
/// </summary>
public class Token
{
	/// <summary>
	/// The original text slice from the document.
	/// </summary>
	public string Text { get; }

	/// <summary>
	/// A normalized version of the text for dictionary lookups.
	/// </summary>
	public string NormalizedText { get; }

	/// <summary>
	/// The classification of the token.
	/// </summary>
	public TokenType Type { get; }

	/// <summary>
	/// The starting character position in the original document.
	/// </summary>
	public int Start { get; }

	/// <summary>
	/// The ending character position in the original document.
	/// </summary>
	public int End { get; }

	/// <summary>
	/// The final resolved linguistic annotations applied by the morphology analyzer.
	/// Null if not a word, not found, or not yet disambiguated.
	/// </summary>
	public MorphologicalInfo? Morph { get; set; }

	/// <summary>
	/// All possible morphological variants for this word found in the dictionary.
	/// Used later by the disambiguation engine.
	/// </summary>
	public IReadOnlyList<MorphologicalInfo>? MorphologicalCandidates { get; set; }

	public Token(string text, string normalizedText, TokenType type, int start, int end)
	{
		Text = text;
		NormalizedText = normalizedText;
		Type = type;
		Start = start;
		End = end;
	}

	public override string ToString()
	{
		return $"[{Type}] '{Text}' ({Start}-{End})";
	}
}