using Pero.Abstractions.Models.Morphology;

namespace Pero.Abstractions.Models;

/// <summary>
/// A container for morphological annotations of a token.
/// </summary>
public class MorphologicalInfo
{
	public string Lemma { get; }
	public MorphologyTagset Tagset { get; }

	public MorphologicalInfo(string lemma, MorphologyTagset tagset)
	{
		Lemma = lemma;
		Tagset = tagset;
	}
}