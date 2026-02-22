using Pero.Abstractions.Models;
using Pero.Abstractions.Models.Morphology;

namespace Pero.Kernel.Utils;

/// <summary>
/// Provides fluent extension methods for fast and readable morphology checks in rules.
/// </summary>
public static class MorphologyExtensions
{
	public static bool Is(this Token token, PartOfSpeech pos)
	{
		return token.Morph?.Tagset.PartOfSpeech == pos;
	}

	public static bool Has(this Token token, GrammarCase grammarCase)
	{
		return token.Morph?.Tagset.Case == grammarCase;
	}

	public static bool Has(this Token token, GrammarGender gender)
	{
		return token.Morph?.Tagset.Gender == gender;
	}

	public static bool Has(this Token token, GrammarNumber number)
	{
		return token.Morph?.Tagset.Number == number;
	}

	public static bool HasFeature(this Token token, GrammarFeatures feature)
	{
		if (token.Morph == null) return false;
		return (token.Morph.Tagset.Features & feature) == feature;
	}

	public static bool IsUnknown(this Token token)
	{
		return token.Type == TokenType.Word && token.Morph == null;
	}
}