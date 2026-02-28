using Pero.Abstractions.Contracts;
using Pero.Abstractions.Models;
using Pero.Abstractions.Telemetry;

namespace Pero.Kernel.Rules;

public abstract class BaseGrammarRule : IRule
{
	public abstract string Id { get; }
	public abstract IssueCategory Category { get; }
	public abstract IssueSeverity Severity { get; }

	public IEnumerable<TextIssue> Check(Sentence sentence, ITelemetryTracker telemetry)
	{
		if (sentence.Tokens.Count == 0)
		{
			yield break;
		}

		using (telemetry.Measure($"Rule.{Id}"))
		{
			foreach (var issue in Analyze(sentence))
			{
				issue.RuleId = Id;
				issue.Category = Category;
				issue.Severity = Severity;
				yield return issue;
			}
		}
	}

	protected abstract IEnumerable<TextIssue> Analyze(Sentence sentence);

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