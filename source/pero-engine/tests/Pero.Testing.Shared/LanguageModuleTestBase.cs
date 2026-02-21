using FluentAssertions;
using Pero.Abstractions.Contracts;
using Pero.Kernel.Pipeline;
using Pero.Testing.Shared.Assertions;
using Pero.Testing.Shared.Data;

namespace Pero.Testing.Shared;

/// <summary>
/// Base class for testing concrete Language Modules.
/// Provides methods for testing Issues (via Golden Master) and Tokenization.
/// </summary>
public abstract class LanguageModuleTestBase
{
	private readonly AnalysisPipeline _pipeline;
	private readonly ILanguageModule _module;

	protected LanguageModuleTestBase()
	{
		_module = CreateModule();
		_pipeline = new AnalysisPipeline(_module);
	}

	protected abstract ILanguageModule CreateModule();

	/// <summary>
	/// Validates the analysis results against the JSON test case.
	/// </summary>
	/// <param name="targetRuleId">
	/// If provided, filters actual results to only this RuleId. 
	/// If null, checks ALL rules (Integration Mode).
	/// </param>
	protected void AssertGolden(string? targetRuleId, TestCase testCase)
	{
		var result = _pipeline.Run(testCase.Text);

		var actualIssues = string.IsNullOrEmpty(targetRuleId)
			? result.Issues // Integration mode: check everything
			: result.Issues.Where(i => i.RuleId == targetRuleId).ToList();

		IssueVerifier.Verify(testCase.Text, actualIssues.ToList(), testCase.Issues);
	}

	/// <summary>
	/// Helper to verify tokenization logic specifically.
	/// Useful when debugging why a rule isn't firing.
	/// </summary>
	protected void AssertTokenization(string text, params string[] expectedTokenTexts)
	{
		var tokenizer = _module.CreateTokenizer();
		var cleaner = _module.CreateTextCleaner();
		var preTokenizer = _module.CreatePreTokenizer();

		// Manual mini-pipeline for tokenization only
		var clean = cleaner.Clean(text);
		var fragments = preTokenizer.Scan(clean);
		var tokens = new List<string>();

		foreach (var frag in fragments)
		{
			// Using the actual tokenizer logic
			if (frag.Type == Abstractions.Models.FragmentType.Raw)
			{
				tokens.AddRange(tokenizer.Tokenize(frag).Select(t => t.Text));
			}
			else
			{
				tokens.Add(frag.Text);
			}
		}

		tokens.Should().ContainInOrder(expectedTokenTexts);
		tokens.Should().HaveSameCount(expectedTokenTexts);
	}
}