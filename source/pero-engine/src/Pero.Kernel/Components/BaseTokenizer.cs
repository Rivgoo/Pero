using System.Text;
using Pero.Abstractions.Contracts;
using Pero.Abstractions.Models;

namespace Pero.Kernel.Components;

public abstract class BaseTokenizer : ITokenizer
{
	private const char _dotCharacter = '.';
	private const int _ellipsisLength = 3;

	public IEnumerable<Token> Tokenize(TextFragment fragment)
	{
		if (fragment.Type != FragmentType.Raw) yield break;

		var text = fragment.Text;
		var cursor = 0;

		while (cursor < text.Length)
		{
			if (!Rune.TryGetRuneAt(text, cursor, out var rune))
				rune = new Rune(text[cursor]);

			var start = cursor;

			if (Rune.IsWhiteSpace(rune))
			{
				cursor = ConsumeWhile(text, cursor, (t, c, r) => Rune.IsWhiteSpace(r));
				yield return CreateToken(text, start, cursor, fragment.Start, TokenType.Whitespace);
			}
			else if (Rune.IsDigit(rune))
			{
				cursor = ConsumeWhile(text, cursor, (t, c, r) => Rune.IsDigit(r));
				yield return CreateToken(text, start, cursor, fragment.Start, TokenType.Number);
			}
			else if (IsWordCharacter(text, cursor, rune))
			{
				cursor = ConsumeWhile(text, cursor, IsWordCharacter);
				yield return CreateToken(text, start, cursor, fragment.Start, TokenType.Word, normalize: true);
			}
			else if (Rune.IsPunctuation(rune))
			{
				cursor = ConsumePunctuation(text, cursor, rune);
				yield return CreateToken(text, start, cursor, fragment.Start, TokenType.Punctuation);
			}
			else
			{
				cursor += rune.Utf16SequenceLength;
				yield return CreateToken(text, start, cursor, fragment.Start, TokenType.Symbol);
			}
		}
	}

	protected abstract bool IsWordCharacter(string text, int cursor, Rune rune);

	protected virtual string NormalizeWord(string word) => word.ToLowerInvariant();

	private static int ConsumeWhile(string text, int startCursor, Func<string, int, Rune, bool> predicate)
	{
		var cursor = startCursor;

		while (cursor < text.Length && Rune.TryGetRuneAt(text, cursor, out var next) && predicate(text, cursor, next))
			cursor += next.Utf16SequenceLength;

		return cursor;
	}

	private static int ConsumePunctuation(string text, int cursor, Rune currentRune)
	{
		var hasEllipsis = currentRune.Value == _dotCharacter
			&& cursor + (_ellipsisLength - 1) < text.Length
			&& text[cursor + 1] == _dotCharacter
			&& text[cursor + 2] == _dotCharacter;

		if (hasEllipsis) return cursor + _ellipsisLength;

		return cursor + currentRune.Utf16SequenceLength;
	}

	private Token CreateToken(string text, int start, int end, int offset, TokenType type, bool normalize = false)
	{
		var value = text.Substring(start, end - start);
		var normalized = normalize ? NormalizeWord(value) : value;
		return new Token(value, normalized, type, offset + start, offset + end);
	}
}