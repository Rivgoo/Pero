using Pero.Abstractions.Models;

namespace Pero.Abstractions.Contracts;

public sealed class RuleDefinition
{
	public string Id { get; }
	public IssueCategory Category { get; }
	public IssueSeverity Severity { get; }

	public RuleDefinition(string id, IssueCategory category, IssueSeverity severity)
	{
		Id = id;
		Category = category;
		Severity = severity;
	}
}