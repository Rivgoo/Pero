using Pero.Abstractions.Contracts;
using Pero.Abstractions.Models;

namespace Pero.Kernel.Components;

public abstract class BaseSentenceSegmenter : ISentenceSegmenter
{
	protected readonly HashSet<string> Terminators;
	protected readonly HashSet<string> ClosingQuotes;

	protected BaseSentenceSegmenter(
		IEnumerable<string> terminators,
		IEnumerable<string> closingQuotes)
	{
		Terminators = terminators.ToHashSet();
		ClosingQuotes = closingQuotes.ToHashSet();
	}

	public IEnumerable<Sentence> Segment(IEnumerable<Token> tokens)
	{
		var tokenList = tokens.ToList();
		var buffer = new List<Token>();

		for (int i = 0; i < tokenList.Count; i++)
		{
			var current = tokenList[i];
			buffer.Add(current);

			if (!IsPotentialTerminator(current))
			{
				if (IsHardLineBreak(current) && HasSignificantContent(buffer))
				{
					yield return FlushBuffer(buffer);
				}
				continue;
			}

			if (ShouldBreakSentence(tokenList, i))
			{
				i = AbsorbClosingPunctuation(tokenList, i, buffer);
				yield return FlushBuffer(buffer);
			}
		}

		if (buffer.Count > 0)
		{
			yield return FlushBuffer(buffer);
		}
	}

	protected abstract bool ShouldBreakSentence(List<Token> context, int currentIndex);

	protected Token? GetPreviousSignificant(List<Token> context, int index)
	{
		return GetPreviousSignificantWithIndex(context, index).Token;
	}

	protected (Token? Token, int Index) GetPreviousSignificantWithIndex(List<Token> context, int index)
	{
		for (int i = index - 1; i >= 0; i--)
		{
			if (context[i].Type != TokenType.Whitespace) return (context[i], i);
		}
		return (null, -1);
	}

	protected Token? GetNextSignificant(List<Token> context, int index)
	{
		for (int i = index + 1; i < context.Count; i++)
		{
			if (context[i].Type != TokenType.Whitespace) return context[i];
		}
		return null;
	}

	protected bool IsCapitalized(Token? token)
	{
		if (token == null || token.Type != TokenType.Word) return false;
		return char.IsUpper(token.Text[0]);
	}

	protected bool IsLowerCase(Token? token)
	{
		if (token == null || token.Type != TokenType.Word) return false;
		return char.IsLower(token.Text[0]);
	}

	protected bool IsInitial(Token? token)
	{
		return token != null
			   && token.Type == TokenType.Word
			   && token.Text.Length == 1
			   && char.IsUpper(token.Text[0]);
	}

	protected bool IsNumber(Token? token)
	{
		return token != null && token.Type == TokenType.Number;
	}

	protected bool IsInternalPunctuation(Token? token)
	{
		if (token == null || token.Type != TokenType.Punctuation) return false;
		return token.Text == "," || token.Text == ";" || token.Text == ":";
	}

	protected bool IsPotentialTerminator(Token token)
	{
		return token != null && token.Type == TokenType.Punctuation && Terminators.Contains(token.Text);
	}

	private bool IsHardLineBreak(Token token)
	{
		return token.Type == TokenType.Whitespace && token.Text.Contains('\n');
	}

	private int AbsorbClosingPunctuation(List<Token> tokens, int currentIndex, List<Token> buffer)
	{
		int lookahead = currentIndex + 1;
		while (lookahead < tokens.Count)
		{
			var next = tokens[lookahead];
			if (next.Type == TokenType.Whitespace) break;

			if (ClosingQuotes.Contains(next.Text) || Terminators.Contains(next.Text))
			{
				buffer.Add(next);
				lookahead++;
			}
			else
			{
				break;
			}
		}
		return lookahead - 1;
	}

	private static Sentence FlushBuffer(List<Token> buffer)
	{
		var sentence = new Sentence(new List<Token>(buffer));
		buffer.Clear();
		return sentence;
	}

	private static bool HasSignificantContent(List<Token> buffer)
	{
		return buffer.Any(t => t.Type == TokenType.Word || t.Type == TokenType.Number);
	}
}