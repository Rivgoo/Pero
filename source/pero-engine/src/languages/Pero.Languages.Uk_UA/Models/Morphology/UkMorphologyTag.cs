using Pero.Abstractions.Models.Morphology;
using Pero.Languages.Uk_UA.Models.Morphology;

/// <summary>
/// A sealed class serving as a Flyweight object for Morphological Tags.
/// Evaluates to extremely fast reference equality and prevents struct boxing.
/// </summary>
public sealed class UkMorphologyTag : MorphologicalTag, IEquatable<UkMorphologyTag>
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

	public UkMorphologyTag(
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

	public bool Equals(UkMorphologyTag? other)
	{
		if (ReferenceEquals(this, other)) return true;
		if (other is null) return false;

		return PartOfSpeech == other.PartOfSpeech &&
			   Case == other.Case &&
			   Gender == other.Gender &&
			   Number == other.Number &&
			   Animacy == other.Animacy &&
			   Aspect == other.Aspect &&
			   Tense == other.Tense &&
			   Person == other.Person &&
			   Features == other.Features;
	}

	public override bool Equals(object? obj) => Equals(obj as UkMorphologyTag);

	public override int GetHashCode()
	{
		var hash = new HashCode();
		hash.Add((byte)PartOfSpeech);
		hash.Add((byte)Case);
		hash.Add((byte)Gender);
		hash.Add((byte)Number);
		hash.Add((byte)Animacy);
		hash.Add((byte)Aspect);
		hash.Add((byte)Tense);
		hash.Add((byte)Person);
		hash.Add((ushort)Features);
		return hash.ToHashCode();
	}
}