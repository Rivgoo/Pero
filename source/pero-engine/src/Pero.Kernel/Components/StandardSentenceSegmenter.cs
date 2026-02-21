using Pero.Abstractions.Contracts;
using Pero.Abstractions.Models;

namespace Pero.Kernel.Components;

/// <summary>
/// A standard implementation of ISentenceSegmenter that splits sentences
/// based on common terminal punctuation marks.
/// </summary>
public class StandardSentenceSegmenter : ISentenceSegmenter
{
	private readonly IReadOnlySet<string> _terminators;
	private readonly IReadOnlySet<string> _abbreviations;

	public StandardSentenceSegmenter(
		IEnumerable<string>? terminators = null,
		IEnumerable<string>? abbreviations = null)
	{
		_terminators = terminators?.ToHashSet() ?? new HashSet<string> { ".", "!", "?", "..." };
		_abbreviations = abbreviations?.ToHashSet(StringComparer.OrdinalIgnoreCase) ?? new HashSet<string>();
	}

	public IEnumerable<Sentence> Segment(IEnumerable<Token> tokens)
	{
		var sentenceBuffer = new List<Token>();
		var tokenList = tokens.ToList();

		for (int i = 0; i < tokenList.Count; i++)
		{
			var token = tokenList[i];
			sentenceBuffer.Add(token);

			bool isTerminator = token.Type == TokenType.Punctuation && _terminators.Contains(token.Text);
			if (!isTerminator)
			{
				continue;
			}

			// Check for abbreviation case: e.g., "Mr." followed by a lowercase word.
			if (token.Text == "." && IsAbbreviation(sentenceBuffer))
			{
				continue;
			}

			yield return new Sentence(new List<Token>(sentenceBuffer));
			sentenceBuffer.Clear();
		}

		if (sentenceBuffer.Count > 0)
		{
			yield return new Sentence(sentenceBuffer);
		}
	}

	private bool IsAbbreviation(IReadOnlyList<Token> buffer)
	{
		if (buffer.Count < 2) return false;

		// Check if previous token was a word.
		var prevToken = buffer[^2];
		if (prevToken.Type != TokenType.Word) return false;

		// Form the potential abbreviation (e.g., "Mr" + "." -> "Mr.")
		var potentialAbbr = prevToken.Text + ".";
		return _abbreviations.Contains(potentialAbbr);
	}
}