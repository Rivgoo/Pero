namespace Pero.Abstractions.Models.Morphology;

[Flags]
public enum GrammarFeatures : ushort
{
	None = 0,
	Abbreviation = 1 << 0,  // abbr
	Bad = 1 << 1,           // bad
	Substandard = 1 << 2,   // subst
	Rare = 1 << 3,          // rare
	Colloquial = 1 << 4,    // coll
	Archaic = 1 << 5,       // arch
	Slang = 1 << 6,         // slang
	Alternative = 1 << 7,   // alt
	Vulgar = 1 << 8,        // vulg
	Obscene = 1 << 9,       // obsc
	Orthography92 = 1 << 10,// up92
	Orthography19 = 1 << 11,// up19
	Variant = 1 << 12       // var
}
