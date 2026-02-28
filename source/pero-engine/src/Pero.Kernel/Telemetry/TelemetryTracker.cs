using System.Collections.Concurrent;
using System.Diagnostics;
using Pero.Abstractions.Telemetry;

namespace Pero.Kernel.Telemetry;

public class TelemetryTracker : ITelemetryTracker
{
	private readonly ConcurrentDictionary<string, double> metrics = new();

	public IDisposable Measure(string metricName)
	{
		return new TelemetryScope(metricName, this);
	}

	public IReadOnlyDictionary<string, double> GetMetrics()
	{
		return new Dictionary<string, double>(metrics);
	}

	public void Record(string metricName, double elapsedMilliseconds)
	{
		metrics.AddOrUpdate(metricName, elapsedMilliseconds, (key, existing) => existing + elapsedMilliseconds);
	}

	private sealed class TelemetryScope : IDisposable
	{
		private readonly string metricName;
		private readonly TelemetryTracker tracker;
		private readonly long startTimestamp;

		public TelemetryScope(string metricName, TelemetryTracker tracker)
		{
			this.metricName = metricName;
			this.tracker = tracker;
			startTimestamp = Stopwatch.GetTimestamp();
		}

		public void Dispose()
		{
			long endTimestamp = Stopwatch.GetTimestamp();
			double elapsedMs = (endTimestamp - startTimestamp) * 1000.0 / Stopwatch.Frequency;
			tracker.Record(metricName, elapsedMs);
		}
	}
}