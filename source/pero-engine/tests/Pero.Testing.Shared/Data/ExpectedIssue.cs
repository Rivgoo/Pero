namespace Pero.Testing.Shared.Data;

public class ExpectedIssue
{
	public string Original { get; set; } = string.Empty;
	public int Start { get; set; }
	public int End { get; set; }
	public List<string>? Suggestions { get; set; }

	/// <summary>
	/// If true, we check if the actual issue contains MessageArgs matching this dictionary.
	/// </summary>
	public Dictionary<string, string>? Args { get; set; }
}