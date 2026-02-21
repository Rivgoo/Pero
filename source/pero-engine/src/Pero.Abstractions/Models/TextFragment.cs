namespace Pero.Abstractions.Models;

/// <summary>
/// Represents a segment of text identified by the IPreTokenizer.
/// It is either a block of raw text or a recognized technical entity (e.g., a URL).
/// </summary>
public class TextFragment
{
	/// <summary>
	/// The text content of the fragment.
	/// </summary>
	public string Text { get; }

	/// <summary>
	/// The classification of the fragment.
	/// </summary>
	public FragmentType Type { get; }

	/// <summary>
	/// The starting character position in the original document.
	/// </summary>
	public int Start { get; }

	/// <summary>
	/// The ending character position in the original document.
	/// </summary>
	public int End { get; }

	public TextFragment(string text, FragmentType type, int start, int end)
	{
		Text = text;
		Type = type;
		Start = start;
		End = end;
	}
}