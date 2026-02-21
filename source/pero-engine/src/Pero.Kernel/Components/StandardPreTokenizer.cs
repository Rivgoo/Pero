using System.Text.RegularExpressions;
using Pero.Abstractions.Contracts;
using Pero.Abstractions.Models;

namespace Pero.Kernel.Components;

public class StandardPreTokenizer : IPreTokenizer
{
	private readonly Regex _scannerRegex;
	private readonly FragmentType[] _groupIndexToTypeMap;

	public StandardPreTokenizer()
	{
		// 1. Define patterns. Order matters for priority.
		var patterns = new (FragmentType Type, string RegexPattern)[]
		{
            // Markdown formatting (lazy match)
            (FragmentType.MarkdownFormat, @"(?:\*\*[^*\n\r]+\*\*|__[^_\n\r]+__|~~[^~\n\r]+~~|\*[^*\n\r]+\*|_[^_\n\r]+_)"),
            
            // URLs (Standard web links)
            (FragmentType.Url, @"https?://(?:www\.)?[-a-zA-Z0-9@:%._+~#=]{1,256}\.[a-zA-Z0-9()]{1,6}\b(?:[-a-zA-Z0-9()@:%_+.~#?&//=]*)"),
            
            // Emails
            (FragmentType.Email, @"\b[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}\b"),
            
            // Crypto Wallets
            (FragmentType.CryptoWalletAddress, @"\b(?:1[a-km-zA-HJ-NP-Z1-9]{25,34}|3[a-km-zA-HJ-NP-Z1-9]{25,34}|bc1[a-zA-HJ-NP-Z0-9]{39,59}|0x[a-fA-F0-9]{40})\b"),
            
            // GUIDs
            (FragmentType.Guid, @"\b[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}\b"),
            
            // MAC Addresses
            (FragmentType.MacAddress, @"\b(?:[0-9A-Fa-f]{2}[:-]){5}(?:[0-9A-Fa-f]{2})\b"),
            
            // IP Addresses
            (FragmentType.IpAddress, @"\b(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\b"),
            
            // File Paths:
            (FragmentType.FilePath, @"(?:[a-zA-Z]:\\[a-zA-Z0-9_\-\.\\]*[a-zA-Z0-9_\-\\]|(?<!\S)/[a-zA-Z0-9_\-\/.]*[a-zA-Z0-9_\-\/])"),
            
            // GPS Coordinates
            (FragmentType.Coordinates, @"\b[-+]?(?:[1-8]?\d(?:\.\d+)?|90(?:\.0+)?)\s*,\s*[-+]?(?:180(?:\.0+)?|(?:1[0-7]\d|[1-9]?\d)(?:\.\d+)?)\b"),
            
            // Phone Numbers
            (FragmentType.PhoneNumber, @"(?:\+?\d{1,3}[\s-]?)?\(?\d{2,4}\)?[\s-]?\d{3}[\s-]?\d{2}[\s-]?\d{2}\b"),
            
            // Dates
            (FragmentType.Date, @"\b(?:[0-3]?\d[\.\-/][0-1]?\d[\.\-/](?:19|20)?\d{2}|(?:19|20)\d{2}[\.\-/][0-1]?\d[\.\-/][0-3]?\d)\b"),
            
            // Time
            (FragmentType.Time, @"\b(?:[01]?[0-9]|2[0-3]):[0-5][0-9](?::[0-5][0-9])?(?:\s?[AaPp][Mm])?\b"),
            
            // Currency
            (FragmentType.Currency, @"(?:[$£€¥]\s?\d{1,3}(?:[,\s]?\d{3})*(?:\.\d{2})?|\b\d{1,3}(?:[,\s]?\d{3})*(?:,\d{2})?\s?[₴₽$€£¥])"),
            
            // Dimensions
            (FragmentType.Dimensions, @"\b\d{1,5}\s?[xX×]\s?\d{1,5}(?:\s?[xX×]\s?\d{1,5})?\b"),
            
            // Hex Colors
            (FragmentType.HexColor, @"#(?:[0-9a-fA-F]{3}){1,2}\b"),
            
            // Version Numbers
            (FragmentType.VersionNumber, @"\bv?\d+\.\d+\.\d+(?:-[a-zA-Z0-9.]+)?\b"),
            
            // Mentions/Hashtags
            (FragmentType.Mention, @"(?:[@#][a-zA-Z0-9_]+)\b")
		};

		// 2. Build Mega-Regex
		var combinedPattern = string.Join("|", patterns.Select(p => $"(?<{p.Type}>{p.RegexPattern})"));
		_scannerRegex = new Regex(combinedPattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);

		// 3. Build fast lookup map
		var maxGroupIndex = _scannerRegex.GetGroupNumbers().Max();
		_groupIndexToTypeMap = new FragmentType[maxGroupIndex + 1];

		// Initialize default
		Array.Fill(_groupIndexToTypeMap, FragmentType.Raw);

		// Map indices
		foreach (var p in patterns)
		{
			var groupNumber = _scannerRegex.GroupNumberFromName(p.Type.ToString());
			if (groupNumber > 0)
			{
				_groupIndexToTypeMap[groupNumber] = p.Type;
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

		var matches = _scannerRegex.Matches(cleanedText);
		int lastIndex = 0;

		foreach (Match match in matches)
		{
			// Yield raw gap
			if (match.Index > lastIndex)
			{
				yield return new TextFragment(
					text: cleanedText.Substring(lastIndex, match.Index - lastIndex),
					type: FragmentType.Raw,
					start: lastIndex,
					end: match.Index
				);
			}

			// Yield technical fragment
			var fragmentType = ResolveFragmentTypeFast(match);
			yield return new TextFragment(
				text: match.Value,
				type: fragmentType,
				start: match.Index,
				end: match.Index + match.Length
			);

			lastIndex = match.Index + match.Length;
		}

		// Yield tail
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
			if (match.Groups[i].Success)
				return _groupIndexToTypeMap[i];

		return FragmentType.Raw;
	}
}