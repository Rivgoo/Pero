using System.Text.RegularExpressions;
using Pero.Abstractions.Contracts;
using Pero.Abstractions.Models;
using Pero.Kernel.Configuration;

namespace Pero.Kernel.Components;

public class HeuristicCodePreTokenizer : IPreTokenizer
{
	private readonly IPreTokenizer innerTokenizer;
	private readonly Regex codePattern;

	public HeuristicCodePreTokenizer(IPreTokenizer innerTokenizer, PreTokenizerConfig config)
	{
		this.innerTokenizer = innerTokenizer;
		var combined = string.Join("|", config.CodePatterns);
		codePattern = new Regex(combined, RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);
	}

	public IEnumerable<TextFragment> Scan(string cleanedText)
	{
		var matches = codePattern.Matches(cleanedText);
		int lastIndex = 0;

		foreach (Match match in matches)
		{
			if (match.Length == 0) continue;

			if (match.Index > lastIndex)
			{
				var gap = cleanedText.Substring(lastIndex, match.Index - lastIndex);
				foreach (var subFragment in innerTokenizer.Scan(gap))
				{
					yield return new TextFragment(
						subFragment.Text,
						subFragment.Type,
						subFragment.Start + lastIndex,
						subFragment.End + lastIndex
					);
				}
			}

			yield return new TextFragment(
				match.Value,
				FragmentType.CodeSnippet,
				match.Index,
				match.Index + match.Length
			);

			lastIndex = match.Index + match.Length;
		}

		if (lastIndex < cleanedText.Length)
		{
			var gap = cleanedText.Substring(lastIndex);
			foreach (var subFragment in innerTokenizer.Scan(gap))
			{
				yield return new TextFragment(
					subFragment.Text,
					subFragment.Type,
					subFragment.Start + lastIndex,
					subFragment.End + lastIndex
				);
			}
		}
	}
}