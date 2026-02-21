using Pero.Abstractions.Models;

namespace Pero.Kernel.Utils;

/// <summary>
/// A utility class providing helper methods for creating TextIssue objects.
/// This helps ensure consistency and reduces boilerplate in rule implementations.
/// </summary>
public static class IssueFactory
{
	/// <summary>
	/// Creates a TextIssue that is anchored to a single token.
	/// </summary>
	public static TextIssue CreateFrom(
		Token token,
		string ruleId,
		IssueCategory category,
		IEnumerable<string>? suggestions = null)
	{
		return new TextIssue
		{
			RuleId = ruleId,
			Category = category,
			Start = token.Start,
			End = token.End,
			Original = token.Text,
			Suggestions = suggestions?.ToList() ?? new List<string>()
		};
	}

	/// <summary>
	/// Creates a TextIssue that spans a range of tokens, from a start token to an end token.
	/// </summary>
	public static TextIssue CreateSpanning(
		IEnumerable<Token> tokens,
		string ruleId,
		IssueCategory category,
		string documentText,
		IEnumerable<string>? suggestions = null)
	{
		var tokenList = tokens.ToList();
		var startToken = tokenList.First();
		var endToken = tokenList.Last();

		return new TextIssue
		{
			RuleId = ruleId,
			Category = category,
			Start = startToken.Start,
			End = endToken.End,
			Original = documentText.Substring(startToken.Start, endToken.End - startToken.Start),
			Suggestions = suggestions?.ToList() ?? new List<string>()
		};
	}
}