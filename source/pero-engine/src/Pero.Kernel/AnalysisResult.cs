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

	public AnalysisResult(AnalyzedDocument document, IReadOnlyList<TextIssue> issues)
	{
		Document = document;
		Issues = issues;
	}
}