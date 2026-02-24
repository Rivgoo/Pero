using System.Text;

namespace Pero.Languages.Uk_UA.Tools.Console.Services.Typo.Strategies;

public class PhoneticConsonantStrategy : ITypoStrategy
{
	private static readonly (char Expected, char Replacement)[] ConsonantPairs =
	{
		('д', 'т'), ('т', 'д'),
		('з', 'с'), ('с', 'з'),
		('б', 'п'), ('п', 'б'),
		('г', 'х'), ('х', 'г'),
		('г', 'ґ'), ('ґ', 'г'),
		('ж', 'ш'), ('ш', 'ж'),
		('ж', 'з'), ('з', 'ж'),
		('ч', 'ц'), ('ц', 'ч')
	};

	public bool TryGenerate(string word, Random random, out string typo, out string category)
	{
		var possibilities = new List<(int Index, char Replacement)>();

		for (int i = 0; i < word.Length; i++)
		{
			foreach (var pair in ConsonantPairs)
			{
				if (word[i] == pair.Expected) possibilities.Add((i, pair.Replacement));
			}
		}

		if (possibilities.Count == 0)
		{
			typo = string.Empty;
			category = string.Empty;
			return false;
		}

		var choice = possibilities[random.Next(possibilities.Count)];
		var sb = new StringBuilder(word);
		sb[choice.Index] = choice.Replacement;

		typo = sb.ToString();
		category = "Phonetic Consonant Assimilation/Dissimilation";
		return true;
	}
}