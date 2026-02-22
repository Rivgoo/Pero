using System.Runtime.InteropServices;

namespace Pero.Abstractions.Models.Morphology;

/// <summary>
/// A highly optimized 8-byte value type representing all morphological features of a word.
/// Designed for zero-allocation parsing and direct binary serialization.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct MorphologyTagset
{
	public PartOfSpeech PartOfSpeech { get; }
	public GrammarCase Case { get; }
	public GrammarGender Gender { get; }
	public GrammarNumber Number { get; }
	public GrammarAnimacy Animacy { get; }
	public GrammarAspect Aspect { get; }
	public GrammarTense Tense { get; }
	public GrammarPerson Person { get; }
	public GrammarFeatures Features { get; }

	public MorphologyTagset(
		PartOfSpeech partOfSpeech,
		GrammarCase grammarCase = GrammarCase.None,
		GrammarGender gender = GrammarGender.None,
		GrammarNumber number = GrammarNumber.None,
		GrammarAnimacy animacy = GrammarAnimacy.None,
		GrammarAspect aspect = GrammarAspect.None,
		GrammarTense tense = GrammarTense.None,
		GrammarPerson person = GrammarPerson.None,
		GrammarFeatures features = GrammarFeatures.None)
	{
		PartOfSpeech = partOfSpeech;
		Case = grammarCase;
		Gender = gender;
		Number = number;
		Animacy = animacy;
		Aspect = aspect;
		Tense = tense;
		Person = person;
		Features = features;
	}
}