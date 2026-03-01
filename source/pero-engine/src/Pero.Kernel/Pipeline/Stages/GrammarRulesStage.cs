using Pero.Abstractions.Contracts;

namespace Pero.Kernel.Pipeline.Stages;

public class GrammarRulesStage : IPipelineStage
{
	private readonly IEnumerable<IAnalyzer> analyzers;

	public string Name => "GrammarRules";

	public GrammarRulesStage(IEnumerable<IAnalyzer> analyzers)
	{
		this.analyzers = analyzers;
	}

	public void Execute(AnalysisContext context)
	{
		if (context.Document == null) return;

		foreach (var sentence in context.Document.Sentences)
		{
			foreach (var analyzer in analyzers)
			{
				var issues = analyzer.Analyze(sentence, context.DisabledRules, context.Telemetry);
				context.Issues.AddRange(issues);
			}
		}
	}
}