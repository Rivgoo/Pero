using Pero.Abstractions.Models.Morphology;

namespace Pero.Abstractions.Contracts;

/// <summary>
/// Decodes a binary blob from the dictionary into language-specific morphological tags.
/// </summary>
public interface IMorphologyDecoder<TTag> where TTag : MorphologicalTag
{
	TTag[] Decode(byte[] data);
}