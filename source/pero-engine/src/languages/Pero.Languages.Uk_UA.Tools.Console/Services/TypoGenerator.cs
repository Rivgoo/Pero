using Pero.Languages.Uk_UA.Dictionaries;
using System.Text;

namespace Pero.Languages.Uk_UA.Tools.Console.Services;

public class TypoGenerator
{
	private readonly CompiledDictionary _dictionary;
	private readonly Random _rnd = new();

	private static readonly Dictionary<char, char[]> KeyboardNeighbors = new()
	{
		{'й', new[]{'ц','ф','і'}}, {'ц', new[]{'й','у','в','ф','і'}}, {'у', new[]{'ц','к','а','в'}}, {'к', new[]{'у','е','п','а'}},
		{'е', new[]{'к','н','р','п'}}, {'н', new[]{'е','г','о','р'}}, {'г', new[]{'н','ш','л','о','ґ'}}, {'ш', new[]{'г','щ','д','л'}},
		{'щ', new[]{'ш','з','ж','д'}}, {'з', new[]{'щ','х','є','ж'}}, {'х', new[]{'з','ї','є'}}, {'ї', new[]{'х','\'','щ'}},
		{'ф', new[]{'й','ц','я','і'}}, {'і', new[]{'ц','у','ч','я','ф','в'}}, {'в', new[]{'у','к','с','ч','і','а'}}, {'а', new[]{'к','е','м','с','в','п'}},
		{'п', new[]{'е','н','и','м','а','р'}}, {'р', new[]{'н','г','т','и','п','о'}}, {'о', new[]{'г','ш','ь','т','р','л'}}, {'л', new[]{'ш','щ','б','ь','о'}},
		{'д', new[]{'щ','з','ю','б','л'}}, {'ж', new[]{'з','х','.', 'ю'}}, {'є', new[]{'х','ї','ж'}},
		{'я', new[]{'ф','і','ч'}}, {'ч', new[]{'я','і','в','с'}}, {'с', new[]{'ч','в','а','м'}}, {'м', new[]{'с','а','п','и'}},
		{'и', new[]{'м','п','р','т'}}, {'т', new[]{'и','р','о','ь'}}, {'ь', new[]{'т','о','л','б'}}, {'б', new[]{'ь','л','д','ю'}}, {'ю', new[]{'б','д','ж'}}
	};

	// Розширені фонетичні пари (акустичні помилки + асиміляція)
	private static readonly (char, char)[] PhoneticPairs = {
		('е', 'и'), ('и', 'е'),
		('г', 'ґ'), ('ґ', 'г'),
		('і', 'ї'), ('ї', 'і'),
		('о', 'а'), ('а', 'о'), // "молоко" -> "малако"
		('з', 'с'), ('с', 'з'), // "розкинув" -> "роскинув"
		('д', 'т'), ('т', 'д'), // "футбол" -> "фудбол"
		('б', 'п'), ('п', 'б'),
		('ж', 'ш'), ('ш', 'ж'),
		('х', 'г'), ('г', 'х'),
		('ш', 'щ'), ('щ', 'ш'), // "борщ" -> "борш"
		('ц', 'т'), // "тьоця" -> "тьотя"
		('н', 'м')  // "комфорт" -> "конфорт"
	};

	// Приголосні, після яких може стояти м'який знак
	private static readonly char[] SoftConsonants = { 'д', 'т', 'з', 'с', 'ц', 'л', 'н' };

	public TypoGenerator(CompiledDictionary dictionary)
	{
		_dictionary = dictionary;
	}

	public (string Typo, string Type)? Generate(string word)
	{
		if (word.Length < 4) return null;

		var strategies = new List<Func<string, (string, string)?>>
		{
			Transposition,
			Omission,
			InsertionDouble,
			KeyboardSubstitution,
			PhoneticSubstitution,
			ApostropheMutation,
			SoftSignMutation,   // NEW
			PrefixMutation,     // NEW
			IotationMutation    // NEW
		};

		Shuffle(strategies);

		foreach (var strategy in strategies)
		{
			// Даємо кілька спроб для кожної стратегії, якщо перша випадкова позиція не спрацювала
			for (int i = 0; i < 2; i++)
			{
				var result = strategy(word);
				if (result != null)
				{
					// Перевіряємо, чи ми випадково не створили інше правильне слово
					if (!_dictionary.Analyze(result.Value.Item1).Any())
					{
						return result;
					}
				}
			}
		}

		return null;
	}

	// --- Existing Strategies ---

	private (string, string)? Transposition(string word)
	{
		if (word.Length < 3) return null;
		int i = _rnd.Next(0, word.Length - 1); // Дозволяємо і на початку слова
		var sb = new StringBuilder(word);
		(sb[i], sb[i + 1]) = (sb[i + 1], sb[i]);
		return (sb.ToString(), "Transposition");
	}

	private (string, string)? Omission(string word)
	{
		int i = _rnd.Next(1, word.Length); // Першу букву зберігаємо частіше для індексації, але не критично
		return (word.Remove(i, 1), "Omission");
	}

	private (string, string)? InsertionDouble(string word)
	{
		int i = _rnd.Next(0, word.Length);
		char c = word[i];
		return (word.Insert(i, c.ToString()), "Doubling");
	}

	private (string, string)? KeyboardSubstitution(string word)
	{
		var candidates = new List<int>();
		for (int k = 0; k < word.Length; k++)
		{
			if (KeyboardNeighbors.ContainsKey(word[k])) candidates.Add(k);
		}

		if (candidates.Count == 0) return null;

		int i = candidates[_rnd.Next(candidates.Count)];
		char original = word[i];
		var neighbors = KeyboardNeighbors[original];
		char replacement = neighbors[_rnd.Next(neighbors.Length)];

		var sb = new StringBuilder(word);
		sb[i] = replacement;
		return (sb.ToString(), "Keyboard");
	}

	private (string, string)? PhoneticSubstitution(string word)
	{
		// Знаходимо всі позиції, де можлива заміна
		var possibilities = new List<(int Index, char Replacement)>();

		for (int i = 0; i < word.Length; i++)
		{
			foreach (var (orig, repl) in PhoneticPairs)
			{
				if (word[i] == orig)
				{
					possibilities.Add((i, repl));
				}
			}
		}

		if (possibilities.Count == 0) return null;

		var chosen = possibilities[_rnd.Next(possibilities.Count)];
		var sb = new StringBuilder(word);
		sb[chosen.Index] = chosen.Replacement;

		return (sb.ToString(), "Phonetic");
	}

	private (string, string)? ApostropheMutation(string word)
	{
		if (word.Contains('\'') || word.Contains('’'))
		{
			return (word.Replace("\'", "").Replace("’", ""), "Apostrophe Missed");
		}

		char[] labials = { 'б', 'п', 'в', 'м', 'ф', 'р' };
		char[] iotated = { 'я', 'ю', 'є', 'ї' };

		var candidates = new List<int>();
		for (int i = 0; i < word.Length - 1; i++)
		{
			if (labials.Contains(word[i]) && iotated.Contains(word[i + 1]))
			{
				candidates.Add(i + 1);
			}
		}

		if (candidates.Count > 0)
		{
			int idx = candidates[_rnd.Next(candidates.Count)];
			return (word.Insert(idx, "'"), "Apostrophe Extra");
		}

		return null;
	}

	// --- New Strategies ---

	private (string, string)? SoftSignMutation(string word)
	{
		// 1. Видалення існуючого м'якого знака
		int softSignIdx = word.IndexOf('ь');
		if (softSignIdx != -1)
		{
			return (word.Remove(softSignIdx, 1), "Soft Sign Omission");
		}

		// 2. Вставка зайвого м'якого знака (після д, т, з, с, ц, л, н)
		var candidates = new List<int>();
		for (int i = 0; i < word.Length; i++)
		{
			if (SoftConsonants.Contains(word[i]))
			{
				// Не вставляти, якщо далі вже є м'який знак або це кінець слова
				if (i < word.Length - 1 && word[i + 1] != 'ь')
				{
					candidates.Add(i + 1);
				}
				else if (i == word.Length - 1)
				{
					candidates.Add(i + 1);
				}
			}
		}

		if (candidates.Count > 0)
		{
			int idx = candidates[_rnd.Next(candidates.Count)];
			return (word.Insert(idx, "ь"), "Soft Sign Extra");
		}

		return null;
	}

	private (string, string)? PrefixMutation(string word)
	{
		if (word.StartsWith("з") && word.Length > 1)
		{
			// Правило "Кафе Птах": перед к, п, т, ф, х пишемо с
			char next = word[1];
			if ("кптфх".Contains(next)) return ("с" + word.Substring(1), "Prefix Z->S Correct"); // Це виправить на правильно, але ми генеруємо тайпо
			return ("с" + word.Substring(1), "Prefix Z->S Error");
		}

		if (word.StartsWith("с") && word.Length > 1)
		{
			return ("з" + word.Substring(1), "Prefix S->Z Error");
		}

		if (word.StartsWith("пре")) return ("при" + word.Substring(3), "Prefix Pre->Pry");
		if (word.StartsWith("при")) return ("пре" + word.Substring(3), "Prefix Pry->Pre");

		return null;
	}

	private (string, string)? IotationMutation(string word)
	{
		// я <-> а, ю <-> у, є <-> е
		var pairs = new (char, char)[] { ('я', 'а'), ('а', 'я'), ('ю', 'у'), ('у', 'ю'), ('є', 'е'), ('е', 'є') };

		var candidates = new List<(int Index, char Replacement)>();
		for (int i = 0; i < word.Length; i++)
		{
			foreach (var (src, dst) in pairs)
			{
				if (word[i] == src) candidates.Add((i, dst));
			}
		}

		if (candidates.Count > 0)
		{
			var chosen = candidates[_rnd.Next(candidates.Count)];
			var sb = new StringBuilder(word);
			sb[chosen.Index] = chosen.Replacement;
			return (sb.ToString(), "Iotation Error");
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