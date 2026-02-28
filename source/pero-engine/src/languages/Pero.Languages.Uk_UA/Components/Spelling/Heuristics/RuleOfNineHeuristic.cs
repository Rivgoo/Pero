using Pero.Abstractions.Contracts;

namespace Pero.Languages.Uk_UA.Components.Spelling.Heuristics;

public class RuleOfNineHeuristic : ISpellingHeuristic
{
	public IEnumerable<string> Generate(string word)
	{
		if (word.Contains('і'))
		{
			yield return word.Replace("ді", "ди").Replace("ті", "ти").Replace("зі", "зи")
							 .Replace("сі", "си").Replace("ці", "ци").Replace("чі", "чи")
							 .Replace("ші", "ши").Replace("жі", "жи").Replace("рі", "ри");
		}

		if (word.Contains('и'))
		{
			yield return word.Replace("ди", "ді").Replace("ти", "ті").Replace("зи", "зі")
							 .Replace("си", "сі").Replace("ци", "ці").Replace("чи", "чі")
							 .Replace("ши", "ші").Replace("жи", "жі").Replace("ри", "рі");
		}
	}
}