namespace Pero.Languages.Uk_UA.Models.Morphology;

public enum GrammarCase : byte
{
	None = 0,
	Nominative,     // v_naz
	Genitive,       // v_rod
	Dative,         // v_dav
	Accusative,     // v_zna
	Instrumental,   // v_oru
	Locative,       // v_mis
	Vocative,       // v_kly
	Uninflected,    // nv
	PluraliaTantum  // ns
}
