using System.Text;

namespace Pero.Languages.Uk_UA.Tools.Console.Services.Typo.Strategies;

public class ForeignWordStrategy : ITypoStrategy
{
	private static readonly char[] RuleOfNine = { 'д', 'т', 'з', 'с', 'ц', 'ч', 'ш', 'ж', 'р' };

	public bool TryGenerate(string word, Random random, out string typo, out string category)
	{
		var possibilities = new List<(int Index, char Replacement)>();

		for (int i = 0; i < word.Length - 1; i++)
		{
			if (RuleOfNine.Contains(word[i]))
			{
				if (word[i + 1] == 'и') possibilities.Add((i + 1, 'і'));
				else if (word[i + 1] == 'і') possibilities.Add((i + 1, 'и'));
			}
		}

		if (possibilities.Count > 0)
		{
			var choice = possibilities[random.Next(possibilities.Count)];
			var sb = new StringBuilder(word);
			sb[choice.Index] = choice.Replacement;
			typo = sb.ToString();
			category = "Rule of Nine Violation (Foreign Words)";
			return true;
		}

		typo = string.Empty;
		category = string.Empty;
		return false;
	}
}
