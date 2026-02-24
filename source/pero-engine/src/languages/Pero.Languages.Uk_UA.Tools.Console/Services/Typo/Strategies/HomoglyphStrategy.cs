using System.Text;

namespace Pero.Languages.Uk_UA.Tools.Console.Services.Typo.Strategies;

public class HomoglyphStrategy : ITypoStrategy
{
	private static readonly Dictionary<char, char> CyrillicToLatin = new()
	{
		{'а', 'a'}, {'о', 'o'}, {'е', 'e'}, {'і', 'i'},
		{'р', 'p'}, {'с', 'c'}, {'х', 'x'}, {'у', 'y'}
	};

	public bool TryGenerate(string word, Random random, out string typo, out string category)
	{
		var possibilities = new List<int>();

		for (int i = 0; i < word.Length; i++)
		{
			if (CyrillicToLatin.ContainsKey(word[i])) possibilities.Add(i);
		}

		if (possibilities.Count == 0)
		{
			typo = string.Empty;
			category = string.Empty;
			return false;
		}

		int idx = possibilities[random.Next(possibilities.Count)];
		var sb = new StringBuilder(word);
		sb[idx] = CyrillicToLatin[word[idx]];

		typo = sb.ToString();
		category = "Homoglyph (Cyrillic-Latin mix)";
		return true;
	}
}
