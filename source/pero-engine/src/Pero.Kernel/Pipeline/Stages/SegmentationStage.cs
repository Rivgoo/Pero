using Pero.Abstractions.Contracts;
using Pero.Abstractions.Models;

namespace Pero.Kernel.Pipeline.Stages;

public class SegmentationStage : IPipelineStage
{
	private readonly ISentenceSegmenter segmenter;

	public string Name => "Segmentation";

	public SegmentationStage(ISentenceSegmenter segmenter)
	{
		this.segmenter = segmenter;
	}

	public void Execute(AnalysisContext context)
	{
		var sentences = segmenter.Segment(context.Tokens).ToList();
		context.Document = new AnalyzedDocument(context.RawText, sentences);
	}
}