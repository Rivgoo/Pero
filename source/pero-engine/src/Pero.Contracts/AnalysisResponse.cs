namespace Pero.Contracts
{
	/// <summary>
	/// Represents the result of a text analysis.
	/// </summary>
	public class AnalysisResponse
	{
		public string RequestId { get; set; } = string.Empty;
		public bool IsSuccess { get; set; }
		public List<TextIssue> Issues { get; set; } = new();
		public double? ProcessingTimeMs { get; set; }
	}
}