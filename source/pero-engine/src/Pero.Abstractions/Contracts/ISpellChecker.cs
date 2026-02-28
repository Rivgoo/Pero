using Pero.Abstractions.Models;
using Pero.Abstractions.Telemetry;

namespace Pero.Abstractions.Contracts;

public interface ISpellChecker
{
	IEnumerable<TextIssue> Check(AnalyzedDocument document, ITelemetryTracker telemetry);
}