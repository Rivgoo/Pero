using Pero.Abstractions.Contracts;
using Pero.Abstractions.Models;

namespace Pero.Kernel.Components.Segmentation;

public interface ISentenceBoundaryRule
{
	SentenceBoundaryDecision Check(IReadOnlyList<Token> context, int currentIndex, ISegmentationProfile profile);
}