using Pero.Abstractions.Models;

namespace Pero.Kernel.Utils;

/// <summary>
/// Provides extension methods for the Sentence class to simplify navigation
/// and inspection of the token stream within a rule.
/// </summary>
public static class SentenceExtensions
{
	/// <summary>
	/// Gets the token immediately following the current token in the sentence.
	/// </summary>
	public static Token? GetNextToken(this Sentence sentence, Token current)
	{
		var index = FindIndex(sentence.Tokens, current);
		if (index == -1 || index >= sentence.Tokens.Count - 1)
			return null;

		return sentence.Tokens[index + 1];
	}

	/// <summary>
	/// Gets the token immediately preceding the current token in the sentence.
	/// </summary>
	public static Token? GetPreviousToken(this Sentence sentence, Token current)
	{
		var index = FindIndex(sentence.Tokens, current);
		if (index <= 0)
			return null;

		return sentence.Tokens[index - 1];
	}

	/// <summary>
	/// Gets the next token that is not of type Whitespace.
	/// </summary>
	public static Token? GetNextSignificantToken(this Sentence sentence, Token current)
	{
		var index = FindIndex(sentence.Tokens, current);
		if (index == -1) return null;

		for (int i = index + 1; i < sentence.Tokens.Count; i++)
			if (sentence.Tokens[i].Type != TokenType.Whitespace)
				return sentence.Tokens[i];

		return null;
	}

	/// <summary>
	/// Gets the previous token that is not of type Whitespace.
	/// </summary>
	public static Token? GetPreviousSignificantToken(this Sentence sentence, Token current)
	{
		var index = FindIndex(sentence.Tokens, current);
		if (index == -1) return null;

		for (int i = index - 1; i >= 0; i--)
			if (sentence.Tokens[i].Type != TokenType.Whitespace)
				return sentence.Tokens[i];

		return null;
	}

	/// <summary>
	/// Helper to find the index of a token in the read-only list.
	/// Uses reference equality since Token instances are unique per pipeline run.
	/// </summary>
	private static int FindIndex(IReadOnlyList<Token> tokens, Token target)
	{
		for (int i = 0; i < tokens.Count; i++)
			if (ReferenceEquals(tokens[i], target))
				return i;

		return -1;
	}
}