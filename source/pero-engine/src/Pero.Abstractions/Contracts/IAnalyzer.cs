using Pero.Abstractions.Models;
using Pero.Abstractions.Telemetry;

namespace Pero.Abstractions.Contracts;

/// <summary>
/// Represents a unit of execution that scans text for one or more issues.
/// Unlike the old IRule, an Analyzer can report multiple different RuleIds.
/// </summary>
public interface IAnalyzer
{
	/// <summary>
	/// Diagnostic name for telemetry.
	/// </summary>
	string Name { get; }

	/// <summary>
	/// The list of specific rules this analyzer is capable of detecting.
	/// </summary>
	IReadOnlyCollection<RuleDefinition> SupportedRules { get; }

	/// <summary>
	/// Runs the analysis logic.
	/// </summary>
	/// <param name="sentence">The target sentence.</param>
	/// <param name="disabledRules">Set of Rule IDs to skip.</param>
	/// <param name="telemetry">Tracker for performance metrics.</param>
	IEnumerable<TextIssue> Analyze(Sentence sentence, IReadOnlySet<string> disabledRules, ITelemetryTracker telemetry);
}