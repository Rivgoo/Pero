using Pero.Contracts;
using System.Text.RegularExpressions;

namespace Pero.Core
{
	public class Analyzer
	{
		private static readonly Regex SpaceBeforeCommaRegex = new(@"\s+,", RegexOptions.Compiled);

		public List<TextIssue> Analyze(string text)
		{
			var issues = new List<TextIssue>();

			if (string.IsNullOrWhiteSpace(text))
			{
				return issues;
			}

			var matches = SpaceBeforeCommaRegex.Matches(text);

			foreach (Match match in matches)
			{
				issues.Add(new TextIssue
				{
					RuleId = "TYPO_SPACE_BEFORE_PUNCT",

					Category = IssueCategory.Typography,
					Severity = IssueSeverity.Warning,

					Start = match.Index,
					End = match.Index + match.Length,

					Original = match.Value,
					Suggestions = new List<string> { "," },

					MessageArgs = new Dictionary<string, string>
					{
						{ "punct", "," }
					}
				});
			}

			return issues;
		}
	}
}