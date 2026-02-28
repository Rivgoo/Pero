using FluentAssertions;
using Pero.Abstractions.Contracts;
using Pero.Kernel.Pipeline;
using Pero.Testing.Shared.Assertions;
using Pero.Testing.Shared.Data;

namespace Pero.Testing.Shared;

public abstract class LanguageModuleTestBase
{
	private readonly AnalysisPipeline pipeline;
	private readonly ILanguageModule module;

	protected LanguageModuleTestBase()
	{
		module = CreateModule();
		pipeline = new AnalysisPipeline(module);
	}

	protected abstract ILanguageModule CreateModule();

	protected void AssertGolden(string? targetRuleId, TestCase testCase)
	{
		var result = pipeline.Run(testCase.Text);

		var actualIssues = string.IsNullOrEmpty(targetRuleId)
			? result.Issues
			: result.Issues.Where(i => i.RuleId == targetRuleId).ToList();

		IssueVerifier.Verify(testCase.Text, actualIssues.ToList(), testCase.Issues);
	}

	protected void AssertTokenization(string text, params string[] expectedTokenTexts)
	{
		var tokenizer = module.CreateTokenizer();
		var cleaner = module.CreateTextCleaner();
		var preTokenizer = module.CreatePreTokenizer();

		var clean = cleaner.Clean(text);
		var fragments = preTokenizer.Scan(clean);
		var tokens = new List<string>();

		foreach (var frag in fragments)
		{
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
