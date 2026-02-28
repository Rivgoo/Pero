using System.Text.RegularExpressions;
using Pero.Abstractions.Contracts;

namespace Pero.Languages.Uk_UA.Components.Spelling.Heuristics;

public class PrefixHeuristic : ISpellingHeuristic
{
	private readonly Regex cafePtahRegex = new("^с[кптфх]", RegexOptions.Compiled);
	private readonly Regex zCafePtahRegex = new("^з[кптфх]", RegexOptions.Compiled);

	public IEnumerable<string> Generate(string word)
	{
		if (word.StartsWith("с") && !cafePtahRegex.IsMatch(word)) yield return "з" + word[1..];
		if (word.StartsWith("з") && zCafePtahRegex.IsMatch(word)) yield return "с" + word[1..];
		if (word.StartsWith("рос")) yield return "роз" + word[3..];
		if (word.StartsWith("бес")) yield return "без" + word[3..];
	}
}