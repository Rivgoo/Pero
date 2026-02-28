namespace Pero.Kernel.Pipeline;

public class PipelineStageTelemetryDecorator : IPipelineStage
{
	private readonly IPipelineStage innerStage;

	public string Name => innerStage.Name;

	public PipelineStageTelemetryDecorator(IPipelineStage innerStage)
	{
		this.innerStage = innerStage;
	}

	public void Execute(AnalysisContext context)
	{
		using (context.Telemetry.Measure($"Stage.{Name}"))
		{
			innerStage.Execute(context);
		}
	}
}