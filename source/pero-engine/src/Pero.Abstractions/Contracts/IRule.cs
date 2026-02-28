using Pero.Abstractions.Models;
using Pero.Abstractions.Telemetry;

namespace Pero.Abstractions.Contracts;

public interface IRule
{
	string Id { get; }
	IEnumerable<TextIssue> Check(Sentence sentence, ITelemetryTracker telemetry);
}