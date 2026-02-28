namespace Pero.Abstractions.Telemetry;

public interface ITelemetryTracker
{
	IDisposable Measure(string metricName);
	IReadOnlyDictionary<string, double> GetMetrics();
}