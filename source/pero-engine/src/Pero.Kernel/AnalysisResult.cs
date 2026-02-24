using Pero.Abstractions.Models;

namespace Pero.Kernel;

/// <summary>
/// Represents the complete output of an analysis pipeline run.
/// </summary>
public class AnalysisResult
{
	/// <summary>
	/// The fully processed document, including sentences and enriched tokens.
	/// </summary>
	public AnalyzedDocument Document { get; }

	/// <summary>
	/// A list of all issues found in the document.
	/// </summary>
	public IReadOnlyList<TextIssue> Issues { get; }

	/// <summary>
	/// Technical execution metrics. Null if telemetry was disabled during the run.
	/// </summary>
	public AnalysisTelemetry? Telemetry { get; }

	public AnalysisResult(AnalyzedDocument document, IReadOnlyList<TextIssue> issues, AnalysisTelemetry? telemetry = null)
	{
		Document = document;
		Issues = issues;
		Telemetry = telemetry;
	}
}