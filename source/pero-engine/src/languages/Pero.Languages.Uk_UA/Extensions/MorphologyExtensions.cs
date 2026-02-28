using Pero.Abstractions.Models;
using Pero.Languages.Uk_UA.Models.Morphology;

namespace Pero.Languages.Uk_UA.Extensions;

/// <summary>
/// Provides fluent extension methods for fast and readable morphology checks in rules.
/// Safely downcasts IMorphologicalTag to UkMorphologyTag.
/// </summary>
public static class MorphologyExtensions
{
	public static bool Is(this Token token, PartOfSpeech pos)
	{
		return token.Morph?.Tag is UkMorphologyTag tag && tag.PartOfSpeech == pos;
	}

	public static bool Has(this Token token, GrammarCase grammarCase)
	{
		return token.Morph?.Tag is UkMorphologyTag tag && tag.Case == grammarCase;
	}

	public static bool Has(this Token token, GrammarGender gender)
	{
		return token.Morph?.Tag is UkMorphologyTag tag && tag.Gender == gender;
	}

	public static bool Has(this Token token, GrammarNumber number)
	{
		return token.Morph?.Tag is UkMorphologyTag tag && tag.Number == number;
	}

	public static bool HasFeature(this Token token, GrammarFeatures feature)
	{
		if (token.Morph?.Tag is not UkMorphologyTag tag) return false;
		return (tag.Features & feature) == feature;
	}

	public static bool IsUnknown(this Token token)
	{
		return token.Type == TokenType.Word && token.Morph == null;
	}
}