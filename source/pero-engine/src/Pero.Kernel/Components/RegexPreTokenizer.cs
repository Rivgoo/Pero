using Pero.Abstractions.Contracts;
using Pero.Abstractions.Models;
using System.Text.RegularExpressions;

namespace Pero.Kernel.Components;

/// <summary>
/// A generic implementation of IPreTokenizer that uses a single, combined
/// regular expression with named capture groups to identify technical fragments.
/// </summary>
public class RegexPreTokenizer : IPreTokenizer
{
	private readonly Regex _scannerRegex;
	private readonly IReadOnlyDictionary<string, FragmentType> _groupNameToTypeMap;

	public RegexPreTokenizer(IReadOnlyDictionary<string, string> patterns)
	{
		var combinedPattern = string.Join("|", patterns.Select(kvp => $"(?<{kvp.Key}>{kvp.Value})"));
		_scannerRegex = new Regex(combinedPattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);

		var map = new Dictionary<string, FragmentType>();
		foreach (var key in patterns.Keys)
		{
			if (Enum.TryParse<FragmentType>(key, true, out var type))
			{
				map[key] = type;
			}
		}
		_groupNameToTypeMap = map;
	}

	public IEnumerable<TextFragment> Scan(string cleanedText)
	{
		var matches = _scannerRegex.Matches(cleanedText);
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

			var (groupName, group) = GetSuccessfulGroup(match);
			yield return new TextFragment(
				text: group.Value,
				type: _groupNameToTypeMap[groupName],
				start: group.Index,
				end: group.Index + group.Length
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

	private (string Name, Group Group) GetSuccessfulGroup(Match match)
	{
		foreach (var groupName in _groupNameToTypeMap.Keys)
		{
			if (match.Groups[groupName].Success)
			{
				return (groupName, match.Groups[groupName]);
			}
		}
		// This should be unreachable if the regex is constructed correctly.
		throw new InvalidOperationException("Regex matched but no successful group was found.");
	}
}