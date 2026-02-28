using Pero.Abstractions.Contracts;

namespace Pero.Kernel.Pipeline.Stages;

public class GrammarRulesStage : IPipelineStage
{
	private readonly IEnumerable<IRule> rules;

	public string Name => "GrammarRules";

	public GrammarRulesStage(IEnumerable<IRule> rules)
	{
		this.rules = rules;
	}

	public void Execute(AnalysisContext context)
	{
		if (context.Document == null) return;

		foreach (var sentence in context.Document.Sentences)
		{
			foreach (var rule in rules)
			{
				context.Issues.AddRange(rule.Check(sentence, context.Telemetry));
			}
		}
	}
}