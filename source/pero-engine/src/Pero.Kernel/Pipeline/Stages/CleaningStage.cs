using Pero.Abstractions.Contracts;

namespace Pero.Kernel.Pipeline.Stages;

public class CleaningStage : IPipelineStage
{
	private readonly ITextCleaner cleaner;

	public string Name => "Cleaning";

	public CleaningStage(ITextCleaner cleaner)
	{
		this.cleaner = cleaner;
	}

	public void Execute(AnalysisContext context)
	{
		context.CleanedText = cleaner.Clean(context.RawText);
	}
}