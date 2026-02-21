namespace Pero.Abstractions.Models;

/// <summary>
/// Defines the type of a text fragment identified by the pre-tokenizer.
/// </summary>
public enum FragmentType
{
	Raw,
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