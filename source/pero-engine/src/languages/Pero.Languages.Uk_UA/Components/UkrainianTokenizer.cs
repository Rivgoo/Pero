using Pero.Kernel.Components;
using System.Globalization;
using System.Text;
using static System.Net.Mime.MediaTypeNames;

namespace Pero.Languages.Uk_UA.Components;

public class UkrainianTokenizer : BaseTokenizer
{
	private static readonly CultureInfo UkCulture = new("uk-UA");

	protected override bool IsWordCharacter(string text, int cursor, Rune rune)
	{
		if (Rune.IsLetter(rune)) return true;

		if (IsApostrophe(rune))
		{
			if (cursor > 0 && cursor + rune.Utf16SequenceLength < text.Length)
			{
				Rune.DecodeLastFromUtf16(text.AsSpan(0, cursor), out var prevRune, out _);
				Rune.DecodeFromUtf16(text.AsSpan(cursor + rune.Utf16SequenceLength), out var nextRune, out _);

				return Rune.IsLetter(prevRune) && Rune.IsLetter(nextRune);
			}
		}

		return false;
	}

	protected override string NormalizeWord(string word)
	{
		return word.ToLower(UkCulture);
	}

	private static bool IsApostrophe(Rune rune)
	{
		// Supports standard apostrophe, right single quotation mark, and modifier letter apostrophe
		return rune.Value == '\'' || rune.Value == '’' || rune.Value == 'ʼ';
	}
}