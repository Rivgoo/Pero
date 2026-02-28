using Pero.Abstractions.Contracts;

namespace Pero.Languages.Uk_UA.Components.Spelling.Heuristics;

public class ConsonantSimplificationHeuristic : ISpellingHeuristic
{
	public IEnumerable<string> Generate(string word)
	{
		if (word.Contains("стн")) yield return word.Replace("стн", "сн");
		if (word.Contains("сн")) yield return word.Replace("сн", "стн");
		if (word.Contains("ждн")) yield return word.Replace("ждн", "жн");
		if (word.Contains("здн")) yield return word.Replace("здн", "зн");
		if (word.Contains("тч")) yield return word.Replace("тч", "чч");
		if (word.Contains("шся")) yield return word.Replace("шся", "сся");
	}
}