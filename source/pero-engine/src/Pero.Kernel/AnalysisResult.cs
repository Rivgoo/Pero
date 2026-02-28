using Pero.Abstractions.Models;

namespace Pero.Kernel;

public class AnalysisResult
{
	public AnalyzedDocument Document { get; }
	public IReadOnlyList<TextIssue> Issues { get; }
	public AnalysisTelemetry? Telemetry { get; }

	public AnalysisResult(AnalyzedDocument document, IReadOnlyList<TextIssue> issues, AnalysisTelemetry? telemetry = null)
	{
		Document = document;
		Issues = issues;
		Telemetry = telemetry;
	}
}