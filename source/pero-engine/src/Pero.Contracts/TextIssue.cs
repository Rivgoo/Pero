namespace Pero.Contracts
{
	/// <summary>
	/// Represents a single detected issue in the text.
	/// </summary>
	public class TextIssue
	{
		public string RuleId { get; set; } = string.Empty;
		public IssueCategory Category { get; set; }
		public IssueSeverity Severity { get; set; }
		public int Start { get; set; }
		public int End { get; set; }
		public string Original { get; set; } = string.Empty;
		public List<string> Suggestions { get; set; } = new();
		public Dictionary<string, string>? MessageArgs { get; set; }
	}
}