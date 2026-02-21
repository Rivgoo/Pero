namespace Pero.Testing.Shared.Data.Tokenization;

public class ExpectedToken
{
	public string Text { get; set; } = string.Empty;
	public string NormalizedText { get; set; } = string.Empty;
	public string Type { get; set; } = string.Empty;
	public int Start { get; set; }
	public int End { get; set; }
}