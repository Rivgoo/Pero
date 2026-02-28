using Pero.Kernel.Dictionaries;
using Pero.Languages.Uk_UA.Tools.Console.Services.Typo.Strategies;

namespace Pero.Languages.Uk_UA.Tools.Console.Services.Typo;

public class TypoGenerator
{
	private readonly FstSuffixDictionary<UkMorphologyTag> _dictionary;
	private readonly IReadOnlyList<ITypoStrategy> _strategies;
	private readonly Random _random;

	public TypoGenerator(FstSuffixDictionary<UkMorphologyTag> dictionary)
	{
		_dictionary = dictionary;
		_random = new Random();

		_strategies = new List<ITypoStrategy>
		{
			new PhoneticVowelStrategy(),
			new PhoneticConsonantStrategy(),
			new GeminationStrategy(),
			new SimplificationStrategy(),
			new ApostropheStrategy(),
			new SoftSignStrategy(),
			new PrefixStrategy(),
			new ForeignWordStrategy(),
			new SurzhykSuffixStrategy(),
			new KeyboardTypoStrategy(),
			new HomoglyphStrategy()
		};
	}

	public (string Typo, string Type)? Generate(string word)
	{
		if (string.IsNullOrWhiteSpace(word) || word.Length < 4) return null;

		var shuffledStrategies = _strategies.OrderBy(x => _random.Next()).ToList();

		foreach (var strategy in shuffledStrategies)
		{
			for (int attempt = 0; attempt < 3; attempt++)
			{
				if (strategy.TryGenerate(word, _random, out var typo, out var category))
				{
					if (IsInvalidWord(typo)) return (typo, category);
				}
			}
		}

		return null;
	}

	private bool IsInvalidWord(string typo)
	{
		return !_dictionary.Analyze(typo).Any();
	}
}