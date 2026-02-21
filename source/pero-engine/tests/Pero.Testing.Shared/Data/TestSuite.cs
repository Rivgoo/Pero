namespace Pero.Testing.Shared.Data;

public class TestSuite
{
	/// <summary>
	/// The Rule ID this suite targets. If null, the suite is considered an integration test
	/// checking for multiple rules.
	/// </summary>
	public string? RuleId { get; set; }
	public string Description { get; set; } = string.Empty;
	public List<TestCase> Cases { get; set; } = new();
}
