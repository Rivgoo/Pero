using Pero.Abstractions.Models.Morphology;
using Pero.Kernel.Dictionaries;
using System.Collections.Concurrent;

namespace Pero.Languages.Uk_UA.Components.Caching;

public class LexiconCache
{
	private readonly FstSuffixDictionary<UkMorphologyTag> _dictionary;
	private readonly ConcurrentDictionary<string, IReadOnlyList<MorphologicalInfo>> _cache;

	public LexiconCache(FstSuffixDictionary<UkMorphologyTag> dictionary)
	{
		_dictionary = dictionary;
		_cache = new ConcurrentDictionary<string, IReadOnlyList<MorphologicalInfo>>(StringComparer.Ordinal);
	}

	public IReadOnlyList<MorphologicalInfo> GetCandidates(string normalizedWord)
	{
		return _cache.GetOrAdd(normalizedWord, word =>
		{
			if (!_dictionary.Contains(word))
			{
				return Array.Empty<MorphologicalInfo>();
			}

			return _dictionary.Analyze(word).ToArray();
		});
	}
}