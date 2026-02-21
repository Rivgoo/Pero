namespace Pero.Abstractions.Transport;

/// <summary>
/// Represents the input data for a text analysis request coming from the browser.
/// </summary>
public class AnalysisRequest
{
	/// <summary>
	/// A unique identifier for the request, used to correlate responses in asynchronous scenarios.
	/// </summary>
	public string RequestId { get; set; } = string.Empty;

	/// <summary>
	/// The text content to be analyzed.
	/// </summary>
	public string Text { get; set; } = string.Empty;

	/// <summary>
	/// The target language code (e.g., "uk-UA").
	/// </summary>
	public string LanguageCode { get; set; } = string.Empty;

	/// <summary>
	/// Optional list of rule IDs to disable for this specific request.
	/// </summary>
	public List<string>? DisabledRules { get; set; }
}