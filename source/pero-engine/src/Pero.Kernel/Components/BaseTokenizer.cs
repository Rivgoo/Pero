using Pero.Abstractions.Contracts;
using Pero.Abstractions.Models;

namespace Pero.Kernel.Components;

/// <summary>
/// An abstract base class providing a default implementation for splitting
/// a raw text fragment into Words, Whitespace, and Punctuation tokens.
/// Language-specific tokenizers should inherit from this class.
/// </summary>
public abstract class BaseTokenizer : ITokenizer
{
	public IEnumerable<Token> Tokenize(TextFragment fragment)
	{
		if (fragment.Type != FragmentType.Raw)
		{
			// This tokenizer is only designed to handle raw text fragments.
			// Technical fragments are handled by the pipeline directly.
			yield break;
		}

		int cursor = 0;
		var text = fragment.Text;

		while (cursor < text.Length)
		{
			var character = text[cursor];
			var start = cursor;

			if (char.IsWhiteSpace(character))
			{
				while (cursor < text.Length && char.IsWhiteSpace(text[cursor]))
				{
					cursor++;
				}
				var value = text.Substring(start, cursor - start);
				yield return new Token(value, value, TokenType.Whitespace, fragment.Start + start, fragment.Start + cursor);
			}
			else if (IsWordCharacter(character))
			{
				while (cursor < text.Length && IsWordCharacter(text[cursor]))
				{
					cursor++;
				}
				var value = text.Substring(start, cursor - start);
				yield return new Token(value, value.ToLowerInvariant(), TokenType.Word, fragment.Start + start, fragment.Start + cursor);
			}
			else // Assume Punctuation or Symbol
			{
				// TODO: More sophisticated symbol/punctuation grouping could be added here.
				cursor++;
				var value = text.Substring(start, 1);
				yield return new Token(value, value, TokenType.Punctuation, fragment.Start + start, fragment.Start + cursor);
			}
		}
	}

	/// <summary>
	/// When overridden in a derived class, determines if a character is part
	/// of a word. This allows language-specific handling of apostrophes or hyphens.
	/// </summary>
	protected abstract bool IsWordCharacter(char c);
}