using Pero.Abstractions.Contracts;
using Pero.Abstractions.Models;

namespace Pero.Kernel.Components.Segmentation.Rules;

public class ExclamationQuestionBoundaryRule : ISentenceBoundaryRule
{
	public SentenceBoundaryDecision Check(IReadOnlyList<Token> context, int currentIndex, ISegmentationProfile profile)
	{
		var terminator = context[currentIndex].Text;

		if (terminator.Contains('!') || terminator.Contains('?'))
		{
			return SentenceBoundaryDecision.Break;
		}

		return SentenceBoundaryDecision.Pass;
	}
}