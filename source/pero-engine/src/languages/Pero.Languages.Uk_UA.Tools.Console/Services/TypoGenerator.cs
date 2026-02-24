using Pero.Languages.Uk_UA.Dictionaries;

namespace Pero.Languages.Uk_UA.Tools.Console.Services;

public class TypoGenerator
{
	private readonly CompiledDictionary _dictionary;
	private readonly Random _rnd = new();

	private static readonly Dictionary<char, char[]> KeyboardNeighbors = new()
	{
		{'й', new[]{'ц','ф','і','q','w','a'}}, {'ц', new[]{'й','у','ф','в','w','e','s'}},
		{'у', new[]{'ц','к','в','а','e','r','d'}}, {'к', new[]{'у','е','а','п','r','t','f'}},
		{'е', new[]{'к','н','п','р','t','y','g'}}, {'н', new[]{'е','г','р','о','y','u','h'}},
		{'г', new[]{'н','ш','о','л','u','i','j'}}, {'ш', new[]{'г','щ','л','д','i','o','k'}},
		{'щ', new[]{'ш','з','д','ж','o','p','l'}}, {'з', new[]{'щ','х','ж','є','p','[',';'}},
		{'ф', new[]{'й','ц','і','я','a','s','z'}}, {'і', new[]{'ц','у','ф','в','я','ч','s','d','x'}},
		{'в', new[]{'у','к','і','а','ч','с','d','f','c'}}, {'а', new[]{'к','е','в','п','с','м','f','g','v'}},
		{'п', new[]{'е','н','а','р','м','и','g','h','b'}}, {'р', new[]{'н','г','п','о','и','т','h','j','n'}},
		{'о', new[]{'г','ш','р','л','т','ь','j','k','m'}}, {'л', new[]{'ш','щ','о','д','ь','б','k','l',','}},
		{'д', new[]{'щ','з','л','ж','б','ю','l',';','.'}}
	};

	private static readonly (char, char)[] PhoneticPairs = {
		('е', 'и'), ('и', 'е'), ('г', 'ґ'), ('ґ', 'г'),
		('і', 'ї'), ('ї', 'і'), ('о', 'а'), ('а', 'о')
	};

	public TypoGenerator(CompiledDictionary dictionary)
	{
		_dictionary = dictionary;
	}

	public (string Typo, string Type)? Generate(string word)
	{
		if (word.Length < 4) return null; // Too short, mutations likely create real words

		// Try different mutations in random order until one produces a non-dictionary word
		var strategies = new List<Func<string, (string, string)?>>
		{
			Transposition,
			Omission,
			InsertionDouble,
			KeyboardSubstitution,
			PhoneticSubstitution,
			ApostropheMutation
		};

		Shuffle(strategies);

		foreach (var strategy in strategies)
		{
			var result = strategy(word);
			if (result != null)
			{
				if (!_dictionary.Analyze(result.Value.Item1).Any())
				{
					return result;
				}
			}
		}

		return null;
	}

	private (string, string)? Transposition(string word)
	{
		int i = _rnd.Next(1, word.Length - 2);
		var chars = word.ToCharArray();
		(chars[i], chars[i + 1]) = (chars[i + 1], chars[i]);
		return (new string(chars), "Transposition");
	}

	private (string, string)? Omission(string word)
	{
		int i = _rnd.Next(1, word.Length - 1);
		return (word.Remove(i, 1), "Omission");
	}

	private (string, string)? InsertionDouble(string word)
	{
		int i = _rnd.Next(1, word.Length - 1);
		char c = word[i];
		// Double a character (common typo)
		return (word.Insert(i, c.ToString()), "Doubling");
	}

	private (string, string)? KeyboardSubstitution(string word)
	{
		int i = _rnd.Next(1, word.Length - 1);
		char original = word[i];

		if (KeyboardNeighbors.TryGetValue(original, out var neighbors))
		{
			char replacement = neighbors[_rnd.Next(neighbors.Length)];
			var chars = word.ToCharArray();
			chars[i] = replacement;
			return (new string(chars), "Keyboard");
		}
		return null;
	}

	private (string, string)? PhoneticSubstitution(string word)
	{
		foreach (var (orig, repl) in PhoneticPairs)
		{
			int idx = word.IndexOf(orig);
			if (idx > 0 && idx < word.Length - 1) // Avoid changing first/last letters
			{
				// 50% chance to skip and try another to ensure randomness
				if (_rnd.Next(2) == 0) continue;

				var chars = word.ToCharArray();
				chars[idx] = repl;
				return (new string(chars), "Phonetic");
			}
		}
		return null;
	}

	private (string, string)? ApostropheMutation(string word)
	{
		if (word.Contains('\''))
		{
			return (word.Replace("'", ""), "Apostrophe Missed");
		}

		// Try injecting apostrophe in valid phonetic places (labial + iotated)
		char[] labials = { 'б', 'п', 'в', 'м', 'ф', 'р' };
		char[] iotated = { 'я', 'ю', 'є', 'ї' };

		for (int i = 0; i < word.Length - 1; i++)
		{
			if (labials.Contains(word[i]) && iotated.Contains(word[i + 1]))
			{
				return (word.Insert(i + 1, "'"), "Apostrophe Extra");
			}
		}
		return null;
	}

	private void Shuffle<T>(List<T> list)
	{
		int n = list.Count;
		while (n > 1)
		{
			n--;
			int k = _rnd.Next(n + 1);
			(list[k], list[n]) = (list[n], list[k]);
		}
	}
}