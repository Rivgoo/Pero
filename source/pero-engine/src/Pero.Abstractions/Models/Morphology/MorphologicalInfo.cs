namespace Pero.Abstractions.Models.Morphology;

/// <summary>
/// A container for morphological annotations of a token.
/// </summary>
public class MorphologicalInfo
{
	public string Lemma { get; }
	public MorphologicalTag Tag { get; }

	public MorphologicalInfo(string lemma, MorphologicalTag tag)
	{
		Lemma = lemma;
		Tag = tag;
	}
}