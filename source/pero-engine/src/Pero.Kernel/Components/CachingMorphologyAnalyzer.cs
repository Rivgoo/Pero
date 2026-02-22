using Pero.Abstractions.Contracts;
using Pero.Abstractions.Models;
using Pero.Abstractions.Models.Morphology;

namespace Pero.Kernel.Components;

/// <summary>
/// A decorator that adds a caching layer over any IMorphologyAnalyzer implementation.
/// This improves performance by avoiding repeated analysis of the same word.
/// </summary>
public class CachingMorphologyAnalyzer : IMorphologyAnalyzer
{
	private readonly IMorphologyAnalyzer _innerAnalyzer;
	private readonly Dictionary<string, MorphologicalInfo?> _cache;

	public CachingMorphologyAnalyzer(IMorphologyAnalyzer innerAnalyzer)
	{
		_innerAnalyzer = innerAnalyzer;
		_cache = new Dictionary<string, MorphologicalInfo?>();
	}

	/// <summary>
	/// Enriches tokens in a sentence, using a cache to retrieve results
	/// for previously seen words.
	/// </summary>
	public void Enrich(Sentence sentence)
	{
		foreach (var token in sentence.Tokens)
		{
			if (token.Type != TokenType.Word || token.Morph != null)
			{
				continue;
			}

			if (_cache.TryGetValue(token.NormalizedText, out var cachedInfo))
			{
				token.Morph = cachedInfo;
			}
			else
			{
				// To analyze a single token, we pass a temporary sentence
				// to the inner analyzer, which it can use for context if needed.
				var tempSentence = new Sentence(new[] { token });
				_innerAnalyzer.Enrich(tempSentence);
				_cache[token.NormalizedText] = token.Morph;
			}
		}
	}
}