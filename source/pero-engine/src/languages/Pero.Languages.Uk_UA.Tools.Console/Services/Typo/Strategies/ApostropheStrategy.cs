namespace Pero.Languages.Uk_UA.Tools.Console.Services.Typo.Strategies;

public class ApostropheStrategy : ITypoStrategy
{
	private static readonly char[] Apostrophes = { '\'', '’', 'ʼ' };
	private static readonly char[] LabialsAndR = { 'б', 'п', 'в', 'м', 'ф', 'р' };
	private static readonly char[] IotatedVowels = { 'я', 'ю', 'є', 'ї' };

	public bool TryGenerate(string word, Random random, out string typo, out string category)
	{
		if (word.IndexOfAny(Apostrophes) >= 0)
		{
			typo = word.Replace("'", "").Replace("’", "").Replace("ʼ", "");
			category = "Omission of Apostrophe";
			return true;
		}

		var candidates = new List<int>();
		for (int i = 0; i < word.Length - 1; i++)
		{
			if (LabialsAndR.Contains(word[i]) && IotatedVowels.Contains(word[i + 1]))
			{
				candidates.Add(i + 1);
			}
		}

		if (candidates.Count > 0)
		{
			int idx = candidates[random.Next(candidates.Count)];
			typo = word.Insert(idx, "'");
			category = "Hypercorrection (False Apostrophe)";
			return true;
		}

		typo = string.Empty;
		category = string.Empty;
		return false;
	}
}
