using Pero.Abstractions.Models.Morphology;
using Pero.Languages.Uk_UA.Dictionaries;

namespace Pero.Languages.Uk_UA.Dictionaries.Fuzzy;

/// <summary>
/// Stubbed for Phase 1. Will be replaced by SymSpell implementation in Phase 2.
/// </summary>
public class FuzzyMatcher
{
	private readonly CompiledDictionary _dictionary;

	public FuzzyMatcher(CompiledDictionary dictionary)
	{
		_dictionary = dictionary;
	}

	public CorrectionCandidate[] Suggest(string targetWord)
	{
		// Removed FST traversal. Returns empty pending SymSpell integration.
		return Array.Empty<CorrectionCandidate>();
	}
}