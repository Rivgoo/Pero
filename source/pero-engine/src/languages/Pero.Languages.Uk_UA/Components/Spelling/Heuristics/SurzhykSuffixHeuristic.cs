using System.Text.RegularExpressions;
using Pero.Abstractions.Contracts;

namespace Pero.Languages.Uk_UA.Components.Spelling.Heuristics;

public class SurzhykSuffixHeuristic : ISpellingHeuristic
{
	private readonly Regex irovatyRegex = new("іроват(и|ь)$", RegexOptions.Compiled);
	private readonly Regex shtykRegex = new("щик(а|у|ом|ів)?$", RegexOptions.Compiled);

	public IEnumerable<string> Generate(string word)
	{
		if (irovatyRegex.IsMatch(word))
			yield return irovatyRegex.Replace(word, "юват$1");

		if (shtykRegex.IsMatch(word))
			yield return shtykRegex.Replace(word, "ник$1");
	}
}