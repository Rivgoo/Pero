using System.Collections.Concurrent;
using Pero.Abstractions.Models;
using Pero.Kernel.Dictionaries;

namespace Pero.Languages.Uk_UA.Components.Caching;

/// <summary>
/// A thread-safe cache specifically for raw dictionary lookups.
/// This prevents caching context-dependent disambiguation results.
/// </summary>
public class LexiconCache
{
	private readonly CompiledDictionary _dictionary;
	private readonly ConcurrentDictionary<string, IReadOnlyList<MorphologicalInfo>> _cache;

	public LexiconCache(CompiledDictionary dictionary)
	{
		_dictionary = dictionary;
		_cache = new ConcurrentDictionary<string, IReadOnlyList<MorphologicalInfo>>(StringComparer.Ordinal);
	}

	public IReadOnlyList<MorphologicalInfo> GetCandidates(string normalizedWord)
	{
		return _cache.GetOrAdd(normalizedWord, word => _dictionary.Analyze(word).ToList());
	}
}