using System.Text;
using Pero.Abstractions.Contracts;
using Pero.Abstractions.Models;
using Pero.Abstractions.Telemetry;

namespace Pero.Kernel.Rules;

public abstract class BaseAnalyzer : IAnalyzer
{
	public abstract string Name { get; }
	public abstract IReadOnlyCollection<RuleDefinition> SupportedRules { get; }

	private Dictionary<string, RuleDefinition>? _ruleLookup;

	public IEnumerable<TextIssue> Analyze(Sentence sentence, IReadOnlySet<string> disabledRules, ITelemetryTracker telemetry)
	{
		if (sentence.Tokens.Count == 0) yield break;

		if (!HasEnabledRules(disabledRules)) yield break;

		EnsureRuleLookupInitialized();

		using (telemetry.Measure($"Analyzer.{Name}"))
		{
			foreach (var issue in Execute(sentence))
			{
				if (!disabledRules.Contains(issue.RuleId))
				{
					yield return issue;
				}
			}
		}
	}

	protected abstract IEnumerable<TextIssue> Execute(Sentence sentence);

	protected RuleDefinition GetRule(string ruleId)
	{
		return _ruleLookup![ruleId];
	}

	protected TextIssue CreateIssue(Token token, string ruleId, string suggestion)
	{
		var rule = GetRule(ruleId);

		return new TextIssue
		{
			RuleId = rule.Id,
			Category = rule.Category,
			Severity = rule.Severity,
			Start = token.Start,
			End = token.End,
			Original = token.Text,
			Suggestions = new List<string> { suggestion }
		};
	}

	protected TextIssue CreateIssue(IReadOnlyList<Token> chunk, string ruleId, string suggestion)
	{
		var rule = GetRule(ruleId);

		return new TextIssue
		{
			RuleId = rule.Id,
			Category = rule.Category,
			Severity = rule.Severity,
			Start = chunk[0].Start,
			End = chunk[^1].End,
			Original = ExtractOriginalText(chunk),
			Suggestions = new List<string> { suggestion }
		};
	}

	private bool HasEnabledRules(IReadOnlySet<string> disabledRules)
	{
		if (disabledRules.Count == 0) return true;

		foreach (var rule in SupportedRules)
		{
			if (!disabledRules.Contains(rule.Id)) return true;
		}

		return false;
	}

	private void EnsureRuleLookupInitialized()
	{
		if (_ruleLookup != null) return;

		_ruleLookup = new Dictionary<string, RuleDefinition>(SupportedRules.Count, StringComparer.Ordinal);
		foreach (var rule in SupportedRules)
		{
			_ruleLookup[rule.Id] = rule;
		}
	}

	private static string ExtractOriginalText(IReadOnlyList<Token> chunk)
	{
		if (chunk.Count == 1) return chunk[0].Text;

		var builder = new StringBuilder();
		for (int i = 0; i < chunk.Count; i++)
		{
			builder.Append(chunk[i].Text);
		}

		return builder.ToString();
	}
}