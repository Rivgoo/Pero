using System.Text;

namespace Pero.Languages.Uk_UA.Tools.Console.Services.Typo.Strategies;

public class PhoneticVowelStrategy : ITypoStrategy
{
	private static readonly (char Expected, char Replacement)[] VowelPairs =
	{
		('е', 'и'), ('и', 'е'),
		('о', 'у'), ('у', 'о'),
		('о', 'а'), ('а', 'о'),
		('і', 'и'), ('и', 'і')
	};

	public bool TryGenerate(string word, Random random, out string typo, out string category)
	{
		var possibilities = new List<(int Index, char Replacement)>();

		for (int i = 1; i < word.Length; i++)
		{
			foreach (var pair in VowelPairs)
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
		category = "Phonetic Vowel Substitution";
		return true;
	}
}