using Pero.Abstractions.Contracts;
using System.Text;

namespace Pero.Kernel.Components;

/// <summary>
/// A highly optimized standard implementation of ITextCleaner.
/// It performs Unicode Normalization (NFC), safely removes destructive control characters,
/// and standardizes exotic whitespace and line breaks in a single pass.
/// </summary>
public class StandardTextCleaner : ITextCleaner
{
	public virtual string Clean(string rawText)
	{
		if (string.IsNullOrEmpty(rawText))
		{
			return string.Empty;
		}

		// 1. Fast Path Normalization
		// IsNormalized() is much faster than calling Normalize() unconditionally.
		bool requiresNormalization = !rawText.IsNormalized(NormalizationForm.FormC);
		string normalizedText = requiresNormalization
			? rawText.Normalize(NormalizationForm.FormC)
			: rawText;

		// 2. Single-pass Character Filtering
		bool isModified = requiresNormalization;
		var sb = new StringBuilder(normalizedText.Length);

		foreach (char c in normalizedText)
		{
			// ASCII Control Characters (0x00 to 0x1F)
			if (c < 0x20)
			{
				// Preserve structural layout characters
				if (c == '\t' || c == '\n' || c == '\r')
				{
					sb.Append(c);
				}
				else
				{
					// Drop destructive/invisible control characters (Null, Bell, Escape, etc.)
					isModified = true;
				}
				continue;
			}

			// O(1) Jump Table for specific Unicode characters
			switch (c)
			{
				// --- Replacements -> Standard Space ---
				case '\u00A0': // Non-breaking space (NBSP)
				case '\u2002': // En space
				case '\u2003': // Em space
				case '\u2009': // Thin space
				case '\u202F': // Narrow no-break space (NNBSP)
					sb.Append(' ');
					isModified = true;
					break;

				// --- Replacements -> Standard Newline ---
				case '\u2028': // Line separator
				case '\u2029': // Paragraph separator
					sb.Append('\n');
					isModified = true;
					break;

				// --- Removals (Invisible formatting, zero-width garbage, bidi marks) ---
				case '\u00AD': // Soft hyphen (SHY)
				case '\u200B': // Zero width space (ZWSP)
				case '\u200E': // Left-to-Right Mark (LTRM)
				case '\u200F': // Right-to-Left Mark (RTLM)
				case '\u2060': // Word joiner
				case '\uFEFF': // Byte order mark (BOM)
				case '\u007F': // Delete character (DEL)
					isModified = true;
					break;

				// --- Default (Preserve everything else) ---
				// This includes valid text, emojis, ZWJ (\u200D), variation selectors (\uFE0F), etc.
				default:
					sb.Append(c);
					break;
			}
		}

		if (!isModified)
			return rawText;

		return sb.ToString();
	}
}