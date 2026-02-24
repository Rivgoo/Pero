using Pero.Abstractions.Models;

namespace Pero.Abstractions.Contracts;

/// <summary>
/// Defines a document-level subsystem for detecting and correcting spelling errors.
/// Runs independently of the standard sentence-level grammar rules.
/// </summary>
public interface ISpellChecker
{
	public bool EnableTelemetry { get; set; }
	public SpellCheckTelemetry? LastTelemetry { get; }

	/// <summary>
	/// Analyzes the entire document to find spelling mistakes and suggest context-aware corrections.
	/// </summary>
	IEnumerable<TextIssue> Check(AnalyzedDocument document);
}