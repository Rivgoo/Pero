namespace Pero.Abstractions.Models;

/// <summary>
/// Represents a single sentence as a sequence of tokens.
/// </summary>
public class Sentence
{
	/// <summary>
	/// The list of tokens that form the sentence.
	/// </summary>
	public IReadOnlyList<Token> Tokens { get; }

	/// <summary>
	/// The starting character position of the sentence in the original document.
	/// </summary>
	public int Start => Tokens.FirstOrDefault()?.Start ?? 0;

	/// <summary>
	/// The ending character position of the sentence in the original document.
	/// </summary>
	public int End => Tokens.LastOrDefault()?.End ?? 0;

	public Sentence(IReadOnlyList<Token> tokens)
	{
		Tokens = tokens;
	}

	public override string ToString()
	{
		return string.Concat(Tokens.Select(t => t.Text));
	}
}