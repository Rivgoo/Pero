using Pero.Abstractions.Contracts;
using Pero.Abstractions.Models;
using Pero.Kernel.Components.Segmentation;

namespace Pero.Kernel.Components;

public abstract class BaseSentenceSegmenter : ISentenceSegmenter
{
	protected readonly ISegmentationProfile Profile;
	private readonly IReadOnlyList<ISentenceBoundaryRule> boundaryRules;

	protected BaseSentenceSegmenter(ISegmentationProfile profile, IEnumerable<ISentenceBoundaryRule> rules)
	{
		Profile = profile;
		boundaryRules = rules.ToList();
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

	protected virtual bool ShouldBreakSentence(List<Token> context, int currentIndex)
	{
		foreach (var rule in boundaryRules)
		{
			var decision = rule.Check(context, currentIndex, Profile);
			if (decision == SentenceBoundaryDecision.Break) return true;
			if (decision == SentenceBoundaryDecision.DoNotBreak) return false;
		}

		return true;
	}

	protected bool IsPotentialTerminator(Token token)
	{
		return token != null && token.Type == TokenType.Punctuation && Profile.Terminators.Contains(token.Text);
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

			if (Profile.ClosingQuotes.Contains(next.Text) || Profile.Terminators.Contains(next.Text))
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