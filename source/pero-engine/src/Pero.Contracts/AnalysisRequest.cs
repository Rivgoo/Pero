namespace Pero.Contracts
{
	/// <summary>
	/// Represents the input data for a text analysis request.
	/// </summary>
	public class AnalysisRequest
	{
		public string RequestId { get; set; } = string.Empty;
		public string Text { get; set; } = string.Empty;
		public string LanguageCode { get; set; } = string.Empty;
		public List<string>? DisabledRules { get; set; }
	}
}