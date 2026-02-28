using Pero.Abstractions.Contracts;

namespace Pero.Languages.Uk_UA.Components.Spelling.Heuristics;

public class VerbEndingHeuristic : ISpellingHeuristic
{
	public IEnumerable<string> Generate(string word)
	{
		if (word.EndsWith("ця")) yield return word[..^2] + "ться";
		if (word.EndsWith("тця")) yield return word[..^3] + "ться";
		if (word.EndsWith("ся")) yield return word[..^2] + "шся";
		if (word.EndsWith("т") || word.EndsWith("ть")) yield return word + "ь";
	}
}