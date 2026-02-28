using Pero.Abstractions.Contracts;
using Pero.Abstractions.Models;
using Pero.Kernel.Components.Segmentation;

namespace Pero.Languages.Uk_UA.Components.Segmentation.Rules;

public class EllipsisBoundaryRule : ISentenceBoundaryRule
{
	public SentenceBoundaryDecision Check(IReadOnlyList<Token> context, int currentIndex, ISegmentationProfile profile)
	{
		var terminator = context[currentIndex].Text;
		if (!terminator.Contains("..")) return SentenceBoundaryDecision.Pass;

		var (prev, _) = context.GetPreviousSignificantWithIndex(currentIndex);
		var next = context.GetNextSignificant(currentIndex);

		if (next.IsLowerCase()) return SentenceBoundaryDecision.DoNotBreak;
		if (prev == null) return SentenceBoundaryDecision.DoNotBreak;

		return SentenceBoundaryDecision.Break;
	}
}