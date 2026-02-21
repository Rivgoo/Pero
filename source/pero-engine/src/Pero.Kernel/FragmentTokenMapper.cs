using Pero.Abstractions.Models;

namespace Pero.Kernel
{
	public static class FragmentTokenMapper
	{
		public static TokenType Map(FragmentType fragmentType) => fragmentType switch
		{
			FragmentType.Url => TokenType.Url,
			FragmentType.Email => TokenType.Email,
			FragmentType.CodeSnippet => TokenType.CodeSnippet,
			FragmentType.Mention => TokenType.Mention,
			FragmentType.Date => TokenType.Date,
			FragmentType.Time => TokenType.Time,
			FragmentType.Currency => TokenType.Currency,
			FragmentType.FilePath => TokenType.FilePath,
			FragmentType.IpAddress => TokenType.IpAddress,
			FragmentType.MacAddress => TokenType.MacAddress,
			FragmentType.PhoneNumber => TokenType.PhoneNumber,
			FragmentType.VersionNumber => TokenType.VersionNumber,
			FragmentType.Guid => TokenType.Guid,
			FragmentType.HexColor => TokenType.HexColor,
			FragmentType.MarkdownFormat => TokenType.MarkdownFormat,
			FragmentType.Coordinates => TokenType.Coordinates,
			FragmentType.CryptoWalletAddress => TokenType.CryptoWalletAddress,
			FragmentType.SocialMediaHandle => TokenType.SocialMediaHandle,
			FragmentType.Dimensions => TokenType.Dimensions,

			_ => TokenType.Unknown
		};
	}
}
