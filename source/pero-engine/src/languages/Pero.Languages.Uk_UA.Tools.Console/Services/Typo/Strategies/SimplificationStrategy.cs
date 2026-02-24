namespace Pero.Languages.Uk_UA.Tools.Console.Services.Typo.Strategies;

public class SimplificationStrategy : ITypoStrategy
{
	private static readonly (string Expected, string Replacement)[] SimplificationPairs =
	{
		("стн", "сн"), ("сн", "стн"),
		("стл", "сл"), ("сл", "стл"),
		("ждн", "жн"), ("жн", "ждн"),
		("здн", "зн"), ("зн", "здн")
	};

	public bool TryGenerate(string word, Random random, out string typo, out string category)
	{
		foreach (var pair in SimplificationPairs.OrderBy(x => random.Next()))
		{
			if (word.Contains(pair.Expected))
			{
				typo = word.Replace(pair.Expected, pair.Replacement);
				category = "Consonant Simplification Error";
				return true;
			}
		}

		typo = string.Empty;
		category = string.Empty;
		return false;
	}
}