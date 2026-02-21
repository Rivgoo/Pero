namespace Pero.Abstractions.Models
{
	/// <summary>
	/// Represents a single problem found in the text.
	/// This object is the final product of the engine and is sent to the UI.
	/// </summary>
	public class TextIssue
	{
		/// <summary>The unique identifier of the rule that detected the issue.</summary>
		public string RuleId { get; set; } = string.Empty;

		/// <summary>The general category of the issue.</summary>
		public IssueCategory Category { get; set; }

		/// <summary>The severity level of the issue.</summary>
		public IssueSeverity Severity { get; set; }

		/// <summary>The starting character position of the issue.</summary>
		public int Start { get; set; }

		/// <summary>The ending character position of the issue.</summary>
		public int End { get; set; }

		/// <summary>The original text fragment that contains the issue.</summary>
		public string Original { get; set; } = string.Empty;

		/// <summary>A list of suggested replacements for the original text.</summary>
		public List<string> Suggestions { get; set; } = new();

		/// <summary>
		/// Dynamic arguments for formatting rich error messages in the UI.
		/// Example: {"punct": ","} for the message "Do not add a space before {punct}".
		/// </summary>
		public Dictionary<string, string>? MessageArgs { get; set; }
	}
}