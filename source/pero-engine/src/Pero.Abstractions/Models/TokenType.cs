namespace Pero.Abstractions.Models
{
	/// <summary>
	/// Defines the type of a token.
	/// </summary>
	public enum TokenType
	{
		Unknown,
		Word,
		Whitespace,
		Punctuation,
		Url,
		Email,
		CodeSnippet,
		Mention,
		Number,
		Symbol
	}
}