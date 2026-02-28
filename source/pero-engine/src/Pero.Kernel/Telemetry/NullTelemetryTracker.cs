using Pero.Abstractions.Telemetry;

namespace Pero.Kernel.Telemetry;

public class NullTelemetryTracker : ITelemetryTracker
{
	private static readonly IReadOnlyDictionary<string, double> EmptyMetrics = new Dictionary<string, double>();
	private static readonly IDisposable EmptyScope = new NullScope();

	public IDisposable Measure(string metricName) => EmptyScope;

	public IReadOnlyDictionary<string, double> GetMetrics() => EmptyMetrics;

	private sealed class NullScope : IDisposable
	{
		public void Dispose() { }
	}
}