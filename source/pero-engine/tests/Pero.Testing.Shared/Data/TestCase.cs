namespace Pero.Testing.Shared.Data;

public class TestCase
{
	public string Text { get; set; } = string.Empty;

	/// <summary>
	/// List of expected issues. If empty, asserts that the text is correct.
	/// </summary>
	public List<ExpectedIssue> Issues { get; set; } = new();

	/// <summary>
	/// Optional: Define expected tokens for tokenization debugging.
	/// </summary>
	public List<string>? ExpectedTokens { get; set; }
}
