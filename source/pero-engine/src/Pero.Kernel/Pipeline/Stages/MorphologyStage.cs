using Pero.Abstractions.Contracts;

namespace Pero.Kernel.Pipeline.Stages;

public class MorphologyStage : IPipelineStage
{
	private readonly IMorphologyAnalyzer analyzer;

	public string Name => "Morphology";

	public MorphologyStage(IMorphologyAnalyzer analyzer)
	{
		this.analyzer = analyzer;
	}

	public void Execute(AnalysisContext context)
	{
		if (context.Document == null) return;

		foreach (var sentence in context.Document.Sentences)
		{
			analyzer.Enrich(sentence);
		}
	}
}