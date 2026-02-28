using Pero.Abstractions.Contracts;

namespace Pero.Kernel.Pipeline.Stages;

public class SpellCheckStage : IPipelineStage
{
	private readonly ISpellChecker spellChecker;

	public string Name => "SpellCheck";

	public SpellCheckStage(ISpellChecker spellChecker)
	{
		this.spellChecker = spellChecker;
	}

	public void Execute(AnalysisContext context)
	{
		if (context.Document == null) return;

		var issues = spellChecker.Check(context.Document, context.Telemetry);
		context.Issues.AddRange(issues);
	}
}