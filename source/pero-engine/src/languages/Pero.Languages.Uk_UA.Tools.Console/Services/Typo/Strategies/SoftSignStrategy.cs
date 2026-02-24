namespace Pero.Languages.Uk_UA.Tools.Console.Services.Typo.Strategies;

public class SoftSignStrategy : ITypoStrategy
{
	private static readonly char[] SoftConsonants = { 'д', 'т', 'з', 'с', 'ц', 'л', 'н', 'р' };

	public bool TryGenerate(string word, Random random, out string typo, out string category)
	{
		int softSignIdx = word.IndexOf('ь');
		if (softSignIdx != -1)
		{
			typo = word.Remove(softSignIdx, 1);
			category = "Omission of Soft Sign";
			return true;
		}

		var candidates = new List<int>();
		for (int i = 0; i < word.Length; i++)
		{
			if (SoftConsonants.Contains(word[i]))
			{
				if (i < word.Length - 1 && word[i + 1] != 'ь') candidates.Add(i + 1);
				else if (i == word.Length - 1) candidates.Add(i + 1);
			}
		}

		if (candidates.Count > 0)
		{
			int idx = candidates[random.Next(candidates.Count)];
			typo = word.Insert(idx, "ь");
			category = "Hypercorrection (False Soft Sign)";
			return true;
		}

		typo = string.Empty;
		category = string.Empty;
		return false;
	}
}
