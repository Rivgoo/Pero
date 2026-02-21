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
		Number,
		Symbol,
		Url,
		Email,
		CodeSnippet,
		Mention,
		Date,
		Time,
		Currency,
		FilePath,
		IpAddress,
		MacAddress,
		PhoneNumber,
		VersionNumber,
		Guid,
		HexColor,
		MarkdownFormat,
		Coordinates,
		CryptoWalletAddress,
		SocialMediaHandle,
		Dimensions
	}
}