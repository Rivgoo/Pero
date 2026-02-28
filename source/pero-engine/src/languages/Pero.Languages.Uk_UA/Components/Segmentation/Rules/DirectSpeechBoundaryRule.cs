using Pero.Abstractions.Contracts;
using Pero.Abstractions.Models;
using Pero.Kernel.Components.Segmentation;

namespace Pero.Languages.Uk_UA.Components.Segmentation.Rules;

public class DirectSpeechBoundaryRule : ISentenceBoundaryRule
{
	public SentenceBoundaryDecision Check(IReadOnlyList<Token> context, int currentIndex, ISegmentationProfile profile)
	{
		int i = currentIndex + 1;
		while (i < context.Count && (profile.ClosingQuotes.Contains(context[i].Text) || context[i].Type == TokenType.Whitespace))
		{
			i++;
		}

		if (i >= context.Count) return SentenceBoundaryDecision.Pass;

		var token = context[i];
		if (token.Text == "—" || token.Text == "–" || token.Text == "-")
		{
			var nextAfterDash = context.GetNextSignificant(i);
			if (nextAfterDash == null) return SentenceBoundaryDecision.Pass;

			if (nextAfterDash.IsLowerCase()) return SentenceBoundaryDecision.DoNotBreak;

			if (profile.StructuralAbbreviations.Contains(nextAfterDash.Text.ToLowerInvariant()))
			{
				return SentenceBoundaryDecision.DoNotBreak;
			}
		}

		return SentenceBoundaryDecision.Pass;
	}
}