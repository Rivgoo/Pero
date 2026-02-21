using Pero.Abstractions.Models;

namespace Pero.Abstractions.Transport;

/// <summary>
/// Represents the result of a text analysis sent back to the browser.
/// </summary>
public class AnalysisResponse
{
	/// <summary>
	/// The ID of the request that triggered this response.
	/// </summary>
	public string RequestId { get; set; } = string.Empty;

	/// <summary>
	/// Indicates whether the analysis completed successfully.
	/// </summary>
	public bool IsSuccess { get; set; }

	/// <summary>
	/// The list of issues detected in the text.
	/// </summary>
	public List<TextIssue> Issues { get; set; } = new();

	/// <summary>
	/// The time taken to process the request (in milliseconds), useful for telemetry.
	/// </summary>
	public double? ProcessingTimeMs { get; set; }

	/// <summary>
	/// Optional error message if IsSuccess is false.
	/// </summary>
	public string? ErrorMessage { get; set; }
}