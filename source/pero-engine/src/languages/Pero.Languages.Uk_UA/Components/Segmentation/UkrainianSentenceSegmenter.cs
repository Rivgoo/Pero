using Pero.Abstractions.Contracts;
using Pero.Kernel.Components;
using Pero.Kernel.Components.Segmentation;
using Pero.Kernel.Components.Segmentation.Rules;
using Pero.Languages.Uk_UA.Components.Segmentation.Rules;

namespace Pero.Languages.Uk_UA.Components;

public class UkrainianSentenceSegmenter : BaseSentenceSegmenter
{
	public UkrainianSentenceSegmenter(ISegmentationProfile profile)
		: base(profile, new List<ISentenceBoundaryRule>
		{
			new PunctuationBoundaryRule(),
			new DirectSpeechBoundaryRule(),
			new ExclamationQuestionBoundaryRule(),
			new EllipsisBoundaryRule(),
			new AbbreviationBoundaryRule()
		})
	{
	}
}