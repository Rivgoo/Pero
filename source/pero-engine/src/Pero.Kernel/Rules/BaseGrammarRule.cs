using Pero.Abstractions.Contracts;
using Pero.Abstractions.Models;

namespace Pero.Kernel.Rules;

/// <summary>
/// A foundation for grammar and spelling rules.
/// Automatically handles the skipping of technical fragments (URLs, code, etc.).
/// </summary>
public abstract class BaseGrammarRule : IRule
{
	public abstract string Id { get; }
	public abstract IssueCategory Category { get; }
	public abstract IssueSeverity Severity { get; }

	public IEnumerable<TextIssue> Check(Sentence sentence)
	{
		if (sentence.Tokens.Count == 0)
		{
			yield break;
		}

		foreach (var issue in Analyze(sentence))
		{
			issue.RuleId = Id;
			issue.Category = Category;
			issue.Severity = Severity;
			yield return issue;
		}
	}

	/// <summary>
	/// Implement this method to define the specific logic of the rule.
	/// </summary>
	protected abstract IEnumerable<TextIssue> Analyze(Sentence sentence);

	/// <summary>
	/// Determines if a token should be ignored by standard linguistic rules.
	/// </summary>
	protected bool IsTechnical(Token token)
	{
		return token.Type switch
		{
			TokenType.Url => true,
			TokenType.Email => true,
			TokenType.CodeSnippet => true,
			TokenType.FilePath => true,
			TokenType.IpAddress => true,
			TokenType.MacAddress => true,
			TokenType.Guid => true,
			TokenType.CryptoWalletAddress => true,
			_ => false
		};
	}
}