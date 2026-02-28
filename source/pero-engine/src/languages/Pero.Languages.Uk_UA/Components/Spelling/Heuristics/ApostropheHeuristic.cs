using Pero.Abstractions.Contracts;

namespace Pero.Languages.Uk_UA.Components.Spelling.Heuristics;

public class ApostropheHeuristic : ISpellingHeuristic
{
	public IEnumerable<string> Generate(string word)
	{
		for (int i = 0; i < word.Length - 1; i++)
		{
			if (IsLabialOrPrefix(word[i]) && IsIotated(word[i + 1]))
			{
				yield return word[..(i + 1)] + "'" + word[(i + 1)..];
			}
		}
	}

	private static bool IsLabialOrPrefix(char c) => c is 'б' or 'п' or 'в' or 'м' or 'ф' or 'р' or 'д' or 'з' or 'с';
	private static bool IsIotated(char c) => c is 'я' or 'ю' or 'є' or 'ї';
}