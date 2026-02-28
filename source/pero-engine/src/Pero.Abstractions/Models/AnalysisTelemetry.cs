namespace Pero.Abstractions.Models;

public class AnalysisTelemetry
{
	public IReadOnlyDictionary<string, double> Metrics { get; }

	public AnalysisTelemetry(IReadOnlyDictionary<string, double> metrics)
	{
		Metrics = metrics;
	}
}