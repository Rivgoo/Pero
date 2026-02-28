using Pero.Abstractions.Contracts;
using Pero.Abstractions.Models;

namespace Pero.Kernel.Components.Segmentation;

public static class TokenNavigationExtensions
{
	public static (Token? Token, int Index) GetPreviousSignificantWithIndex(this IReadOnlyList<Token> context, int index)
	{
		for (int i = index - 1; i >= 0; i--)
		{
			if (context[i].Type != TokenType.Whitespace) return (context[i], i);
		}
		return (null, -1);
	}

	public static Token? GetNextSignificant(this IReadOnlyList<Token> context, int index)
	{
		for (int i = index + 1; i < context.Count; i++)
		{
			if (context[i].Type != TokenType.Whitespace) return context[i];
		}
		return null;
	}

	public static Token? GetNextSignificantSkippingQuotes(this IReadOnlyList<Token> context, int currentIndex, ISegmentationProfile profile)
	{
		for (int i = currentIndex + 1; i < context.Count; i++)
		{
			var t = context[i];
			if (t.Type == TokenType.Whitespace) continue;
			if (profile.ClosingQuotes.Contains(t.Text)) continue;
			return t;
		}
		return null;
	}

	public static bool IsCapitalized(this Token? token) => token != null && token.Type == TokenType.Word && char.IsUpper(token.Text[0]);
	public static bool IsLowerCase(this Token? token) => token != null && token.Type == TokenType.Word && char.IsLower(token.Text[0]);
	public static bool IsNumber(this Token? token) => token != null && token.Type == TokenType.Number;
	public static bool IsInitial(this Token? token) => token != null && token.Type == TokenType.Word && token.Text.Length == 1 && char.IsUpper(token.Text[0]);
	public static bool IsInternalPunctuation(this Token? token) => token != null && token.Type == TokenType.Punctuation && (token.Text == "," || token.Text == ";" || token.Text == ":");
}