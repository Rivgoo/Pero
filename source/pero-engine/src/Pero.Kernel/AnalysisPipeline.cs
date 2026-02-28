using Pero.Abstractions.Contracts;
using Pero.Abstractions.Models;
using Pero.Abstractions.Telemetry;
using Pero.Kernel.Pipeline.Stages;
using Pero.Kernel.Telemetry;

namespace Pero.Kernel.Pipeline;

public class AnalysisPipeline
{
	private readonly IReadOnlyList<IPipelineStage> stages;

	public AnalysisPipeline(ILanguageModule module, bool enableTelemetry = false)
	{
		var baseStages = new List<IPipelineStage>
		{
			new CleaningStage(module.CreateTextCleaner()),
			new TokenizationStage(module.CreatePreTokenizer(), module.CreateTokenizer()),
			new SegmentationStage(module.CreateSentenceSegmenter()),
			new MorphologyStage(module.CreateMorphologyAnalyzer()),
			new SpellCheckStage(module.CreateSpellChecker()),
			new GrammarRulesStage(module.GetRules())
		};

		if (enableTelemetry)
		{
			stages = baseStages.Select(s => new PipelineStageTelemetryDecorator(s)).ToList();
		}
		else
		{
			stages = baseStages;
		}
	}

	public AnalysisResult Run(string rawText, bool enableTelemetry = false)
	{
		var tracker = enableTelemetry ? (ITelemetryTracker)new TelemetryTracker() : new NullTelemetryTracker();
		var context = new AnalysisContext(rawText, tracker);

		using (tracker.Measure("Pipeline.Total"))
		{
			foreach (var stage in stages)
			{
				stage.Execute(context);
			}
		}

		var telemetry = enableTelemetry ? new AnalysisTelemetry(tracker.GetMetrics()) : null;
		return new AnalysisResult(context.Document!, context.Issues, telemetry);
	}
}