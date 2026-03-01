using Pero.Abstractions.Models;
using Pero.Abstractions.Telemetry;

namespace Pero.Kernel.Pipeline;

public class AnalysisContext
{
	public string RawText { get; }
	public string CleanedText { get; set; }
	public List<Token> Tokens { get; set; }
	public AnalyzedDocument? Document { get; set; }
	public List<TextIssue> Issues { get; }
	public ITelemetryTracker Telemetry { get; }
	public IReadOnlySet<string> DisabledRules { get; }

	public AnalysisContext(string rawText, ITelemetryTracker telemetry, IEnumerable<string>? disabledRules = null)
	{
		RawText = rawText;
		CleanedText = rawText;
		Tokens = new List<Token>();
		Issues = new List<TextIssue>();
		Telemetry = telemetry;
		DisabledRules = disabledRules != null
			? new HashSet<string>(disabledRules, StringComparer.Ordinal)
			: new HashSet<string>();
	}
}