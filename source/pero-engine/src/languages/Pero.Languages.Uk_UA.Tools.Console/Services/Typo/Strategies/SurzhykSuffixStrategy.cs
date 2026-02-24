namespace Pero.Languages.Uk_UA.Tools.Console.Services.Typo.Strategies;

public class SurzhykSuffixStrategy : ITypoStrategy
{
	private static readonly (string Expected, string Replacement)[] SuffixPairs =
	{
		("ник", "щик"),
		("яр", "чик"),
		("юва", "ірова"),
		("ува", "ірова"),
		("ов", "ев"),
		("ев", "ов"),
		("ив", "ев")
	};

	public bool TryGenerate(string word, Random random, out string typo, out string category)
	{
		foreach (var pair in SuffixPairs.OrderBy(x => random.Next()))
		{
			if (word.EndsWith(pair.Expected + "ти") || word.EndsWith(pair.Expected) || word.Contains(pair.Expected))
			{
				typo = word.Replace(pair.Expected, pair.Replacement);
				category = "Morphological Surzhyk Suffix";
				return true;
			}
		}

		typo = string.Empty;
		category = string.Empty;
		return false;
	}
}
