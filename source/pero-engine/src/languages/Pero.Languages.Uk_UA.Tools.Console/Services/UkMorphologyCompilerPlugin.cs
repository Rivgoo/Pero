using Pero.Languages.Uk_UA.Models.Morphology;
using Pero.Tools.Compiler.Contracts;

namespace Pero.Languages.Uk_UA.Tools.Console.Services;

public class UkMorphologyCompilerPlugin : IMorphologyCompilerPlugin
{
	private const char TagSeparator = ':';
	private readonly Dictionary<UkMorphologyTag, ushort> registry = new();
	private readonly List<UkMorphologyTag> tagsets = new();

	public ushort GetOrAddTagId(string tagString)
	{
		var tag = ParseTag(tagString);
		if (!registry.TryGetValue(tag, out var tagId))
		{
			tagId = (ushort)tagsets.Count;
			registry[tag] = tagId;
			tagsets.Add(tag);
		}
		return tagId;
	}

	public byte[] SerializeTags()
	{
		using var ms = new MemoryStream();
		using var writer = new BinaryWriter(ms);

		foreach (var t in tagsets)
		{
			writer.Write((byte)t.PartOfSpeech);
			writer.Write((byte)t.Case);
			writer.Write((byte)t.Gender);
			writer.Write((byte)t.Number);
			writer.Write((byte)t.Animacy);
			writer.Write((byte)t.Aspect);
			writer.Write((byte)t.Tense);
			writer.Write((byte)t.Person);
			writer.Write((ushort)t.Features);
		}

		return ms.ToArray();
	}

	private UkMorphologyTag ParseTag(string tagString)
	{
		var parts = tagString.Split(TagSeparator);
		if (parts.Length == 0) return new UkMorphologyTag(PartOfSpeech.Unknown);

		var pos = ParsePartOfSpeech(parts[0]);
		var caseType = GrammarCase.None;
		var gender = GrammarGender.None;
		var number = GrammarNumber.None;
		var animacy = GrammarAnimacy.None;
		var aspect = GrammarAspect.None;
		var tense = GrammarTense.None;
		var person = GrammarPerson.None;
		var features = GrammarFeatures.None;

		for (int i = 1; i < parts.Length; i++)
		{
			var p = parts[i];

			if (TryParseCase(p, out var c)) caseType = c;
			else if (TryParseGender(p, out var g)) gender = g;
			else if (TryParseNumber(p, out var n)) number = n;
			else if (TryParseAnimacy(p, out var a)) animacy = a;
			else if (TryParseAspect(p, out var aspc)) aspect = aspc;
			else if (TryParseTense(p, out var t)) tense = t;
			else if (TryParsePerson(p, out var prs)) person = prs;
			else if (TryParseFeature(p, out var f)) features |= f;
		}

		return new UkMorphologyTag(pos, caseType, gender, number, animacy, aspect, tense, person, features);
	}

	private static PartOfSpeech ParsePartOfSpeech(string tag) => tag switch
	{
		"noun" => PartOfSpeech.Noun,
		"verb" => PartOfSpeech.Verb,
		"adj" => PartOfSpeech.Adjective,
		"adv" => PartOfSpeech.Adverb,
		"prep" => PartOfSpeech.Preposition,
		"conj" => PartOfSpeech.Conjunction,
		"part" => PartOfSpeech.Particle,
		"intj" => PartOfSpeech.Interjection,
		"numr" => PartOfSpeech.Numeral,
		"pron" => PartOfSpeech.Pronoun,
		"noninfl" => PartOfSpeech.NonInflected,
		"onomat" => PartOfSpeech.Onomatopoeia,
		_ => PartOfSpeech.Unknown
	};

	private static bool TryParseCase(string tag, out GrammarCase result)
	{
		result = tag switch
		{
			"v_naz" => GrammarCase.Nominative,
			"v_rod" => GrammarCase.Genitive,
			"v_dav" => GrammarCase.Dative,
			"v_zna" => GrammarCase.Accusative,
			"v_oru" => GrammarCase.Instrumental,
			"v_mis" => GrammarCase.Locative,
			"v_kly" => GrammarCase.Vocative,
			"nv" => GrammarCase.Uninflected,
			"ns" => GrammarCase.PluraliaTantum,
			_ => GrammarCase.None
		};
		return result != GrammarCase.None;
	}

	private static bool TryParseGender(string tag, out GrammarGender result)
	{
		result = tag switch { "m" => GrammarGender.Masculine, "f" => GrammarGender.Feminine, "n" => GrammarGender.Neuter, _ => GrammarGender.None };
		return result != GrammarGender.None;
	}

	private static bool TryParseNumber(string tag, out GrammarNumber result)
	{
		result = tag switch { "s" => GrammarNumber.Singular, "p" => GrammarNumber.Plural, _ => GrammarNumber.None };
		return result != GrammarNumber.None;
	}

	private static bool TryParseAnimacy(string tag, out GrammarAnimacy result)
	{
		result = tag switch { "anim" => GrammarAnimacy.Animate, "inanim" => GrammarAnimacy.Inanimate, "unanim" => GrammarAnimacy.Unspecified, _ => GrammarAnimacy.None };
		return result != GrammarAnimacy.None;
	}

	private static bool TryParseAspect(string tag, out GrammarAspect result)
	{
		result = tag switch { "perf" => GrammarAspect.Perfective, "imperf" => GrammarAspect.Imperfective, _ => GrammarAspect.None };
		return result != GrammarAspect.None;
	}

	private static bool TryParseTense(string tag, out GrammarTense result)
	{
		result = tag switch { "past" => GrammarTense.Past, "pres" => GrammarTense.Present, "futr" => GrammarTense.Future, _ => GrammarTense.None };
		return result != GrammarTense.None;
	}

	private static bool TryParsePerson(string tag, out GrammarPerson result)
	{
		result = tag switch { "1" => GrammarPerson.First, "2" => GrammarPerson.Second, "3" => GrammarPerson.Third, _ => GrammarPerson.None };
		return result != GrammarPerson.None;
	}

	private static bool TryParseFeature(string tag, out GrammarFeatures result)
	{
		result = tag switch
		{
			"abbr" => GrammarFeatures.Abbreviation,
			"bad" => GrammarFeatures.Bad,
			"subst" => GrammarFeatures.Substandard,
			"rare" => GrammarFeatures.Rare,
			"coll" => GrammarFeatures.Colloquial,
			"arch" => GrammarFeatures.Archaic,
			"slang" => GrammarFeatures.Slang,
			"alt" => GrammarFeatures.Alternative,
			"vulg" => GrammarFeatures.Vulgar,
			"obsc" => GrammarFeatures.Obscene,
			"up92" => GrammarFeatures.Orthography92,
			"up19" => GrammarFeatures.Orthography19,
			"var" => GrammarFeatures.Variant,
			_ => GrammarFeatures.None
		};
		return result != GrammarFeatures.None;
	}
}