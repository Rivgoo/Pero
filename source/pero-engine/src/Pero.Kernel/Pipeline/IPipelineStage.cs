namespace Pero.Kernel.Pipeline;

public interface IPipelineStage
{
	string Name { get; }
	void Execute(AnalysisContext context);
}