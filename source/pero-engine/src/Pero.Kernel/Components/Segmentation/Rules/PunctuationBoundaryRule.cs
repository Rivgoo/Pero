using Pero.Abstractions.Contracts;
using Pero.Abstractions.Models;

namespace Pero.Kernel.Components.Segmentation.Rules;

public class PunctuationBoundaryRule : ISentenceBoundaryRule
{
	public SentenceBoundaryDecision Check(IReadOnlyList<Token> context, int currentIndex, ISegmentationProfile profile)
	{
		var next = context.GetNextSignificant(currentIndex);

		if (next == null) return SentenceBoundaryDecision.Break;

		if (next.IsInternalPunctuation()) return SentenceBoundaryDecision.DoNotBreak;

		return SentenceBoundaryDecision.Pass;
	}
}