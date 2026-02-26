using Pero.Abstractions.Models;

namespace Pero.Kernel.Utils;

public static class IssueFactory
{
	public static TextIssue CreateFrom(
		Token token,
		string ruleId,
		IssueCategory category,
		IEnumerable<string>? suggestions = null,
		Dictionary<string, string>? messageArgs = null,
		string? fallbackTitle = null,
		string? fallbackDescription = null)
	{
		return new TextIssue
		{
			RuleId = ruleId,
			Category = category,
			Start = token.Start,
			End = token.End,
			Original = token.Text,
			Suggestions = suggestions?.ToList() ?? new List<string>(),
			MessageArgs = messageArgs,
			FallbackTitle = fallbackTitle,
			FallbackDescription = fallbackDescription
		};
	}

	public static TextIssue CreateSpanning(
		IEnumerable<Token> tokens,
		string ruleId,
		IssueCategory category,
		string documentText,
		IEnumerable<string>? suggestions = null,
		Dictionary<string, string>? messageArgs = null,
		string? fallbackTitle = null,
		string? fallbackDescription = null)
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
			Suggestions = suggestions?.ToList() ?? new List<string>(),
			MessageArgs = messageArgs,
			FallbackTitle = fallbackTitle,
			FallbackDescription = fallbackDescription
		};
	}
}