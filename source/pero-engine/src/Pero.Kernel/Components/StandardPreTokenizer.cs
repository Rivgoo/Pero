using System.Text.RegularExpressions;
using Pero.Abstractions.Contracts;
using Pero.Abstractions.Models;
using Pero.Kernel.Configuration;

namespace Pero.Kernel.Components;

public class StandardPreTokenizer : IPreTokenizer
{
	private readonly Regex scannerRegex;
	private readonly FragmentType[] groupIndexToTypeMap;

	public StandardPreTokenizer(PreTokenizerConfig config)
	{
		var patterns = config.TechnicalPatterns.Select(kvp => (Type: kvp.Key, RegexPattern: kvp.Value)).ToArray();
		var combinedPattern = string.Join("|", patterns.Select(p => $"(?<{p.Type}>{p.RegexPattern})"));

		scannerRegex = new Regex(combinedPattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);

		var maxGroupIndex = scannerRegex.GetGroupNumbers().Max();
		groupIndexToTypeMap = new FragmentType[maxGroupIndex + 1];

		Array.Fill(groupIndexToTypeMap, FragmentType.Raw);

		foreach (var p in patterns)
		{
			var groupNumber = scannerRegex.GroupNumberFromName(p.Type.ToString());
			if (groupNumber > 0)
			{
				groupIndexToTypeMap[groupNumber] = p.Type;
			}
		}
	}

	public IEnumerable<TextFragment> Scan(string cleanedText)
	{
		if (string.IsNullOrWhiteSpace(cleanedText))
		{
			if (cleanedText.Length > 0)
			{
				yield return new TextFragment(cleanedText, FragmentType.Raw, 0, cleanedText.Length);
			}
			yield break;
		}

		var matches = scannerRegex.Matches(cleanedText);
		int lastIndex = 0;

		foreach (Match match in matches)
		{
			if (match.Index > lastIndex)
			{
				yield return new TextFragment(
					text: cleanedText.Substring(lastIndex, match.Index - lastIndex),
					type: FragmentType.Raw,
					start: lastIndex,
					end: match.Index
				);
			}

			var fragmentType = ResolveFragmentTypeFast(match);
			yield return new TextFragment(
				text: match.Value,
				type: fragmentType,
				start: match.Index,
				end: match.Index + match.Length
			);

			lastIndex = match.Index + match.Length;
		}

		if (lastIndex < cleanedText.Length)
		{
			yield return new TextFragment(
				text: cleanedText.Substring(lastIndex),
				type: FragmentType.Raw,
				start: lastIndex,
				end: cleanedText.Length
			);
		}
	}

	private FragmentType ResolveFragmentTypeFast(Match match)
	{
		for (int i = 1; i < match.Groups.Count; i++)
		{
			if (match.Groups[i].Success)
			{
				return groupIndexToTypeMap[i];
			}
		}
		return FragmentType.Raw;
	}
}