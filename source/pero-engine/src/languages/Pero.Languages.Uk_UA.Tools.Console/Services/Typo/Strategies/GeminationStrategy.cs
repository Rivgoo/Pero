using System.Text.RegularExpressions;

namespace Pero.Languages.Uk_UA.Tools.Console.Services.Typo.Strategies;

public class GeminationStrategy : ITypoStrategy
{
	private static readonly Regex DoubleConsonantRegex = new(@"(.)\1", RegexOptions.Compiled);
	private static readonly Regex SingleConsonantRegex = new(@"([нлтячшжц])(?=[аеєиіоуюя])", RegexOptions.Compiled);

	public bool TryGenerate(string word, Random random, out string typo, out string category)
	{
		if (DoubleConsonantRegex.IsMatch(word))
		{
			typo = DoubleConsonantRegex.Replace(word, "$1", 1);
			category = "Loss of Gemination";
			return true;
		}

		var singleMatches = SingleConsonantRegex.Matches(word);
		if (singleMatches.Count > 0)
		{
			var match = singleMatches[random.Next(singleMatches.Count)];
			typo = word.Insert(match.Index, match.Value);
			category = "Hypercorrection (False Gemination)";
			return true;
		}

		typo = string.Empty;
		category = string.Empty;
		return false;
	}
}