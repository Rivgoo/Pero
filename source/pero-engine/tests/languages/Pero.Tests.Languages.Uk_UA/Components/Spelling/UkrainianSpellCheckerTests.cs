using FluentAssertions;
using Pero.Kernel.Pipeline;
using Pero.Languages.Uk_UA;
using Pero.Testing.Shared.Loaders;
using System.Text;
using Xunit.Abstractions;

namespace Pero.Tests.Languages.Uk_UA.Components.Spelling;

public partial class UkrainianSpellCheckerTests
{
	private readonly ITestOutputHelper _output;
	private static readonly AnalysisPipeline _pipeline = new(new UkrainianLanguageModule());

	public UkrainianSpellCheckerTests(ITestOutputHelper output)
	{
		_output = output;
	}

	[Fact]
	public void BatchCheck_ShouldReachAccuracyThreshold()
	{
		// Arrange
		var testCases = LoadAllTestCases();
		var total = testCases.Count;
		var passed = 0;
		var reportBuilder = new StringBuilder();

		reportBuilder.AppendLine($"\n=== SPELL CHECKER ACCURACY REPORT ===");

		// Act
		foreach (var testCase in testCases)
		{
			var result = _pipeline.Run(testCase.Input);
			var issue = result.Issues.FirstOrDefault(i => i.RuleId == "UK_UA_SPELLING_ERROR");

			bool isSuccess = false;
			int foundIndex = -1;

			if (issue != null)
			{
				foundIndex = issue.Suggestions.IndexOf(testCase.Expected);

				if (foundIndex >= 0 && foundIndex <= 2)
				{
					isSuccess = true;
				}
			}

			if (isSuccess)
			{
				passed++;
			}
			else
			{
				string actual = issue?.Suggestions != null && issue.Suggestions.Count > 0
					? $"[{string.Join(", ", issue.Suggestions)}]"
					: "[NO SUGGESTIONS]";

				string status = foundIndex == -1 ? "NOT FOUND" : $"RANK {foundIndex + 1} (Target: Top 3)";

				reportBuilder.AppendLine($"FAIL | {testCase.Name,-25} | In: {testCase.Input,-15} | Exp: {testCase.Expected,-15} | Status: {status,-20} | Found: {actual}");
			}
		}

		double accuracy = (double)passed / total;
		reportBuilder.AppendLine(new string('-', 50));
		reportBuilder.AppendLine($"TOTAL: {total}");
		reportBuilder.AppendLine($"PASSED: {passed}");
		reportBuilder.AppendLine($"FAILED: {total - passed}");
		reportBuilder.AppendLine($"ACCURACY: {accuracy:P2}");
		reportBuilder.AppendLine(new string('=', 50));

		_output.WriteLine(reportBuilder.ToString());

		// Assert
		accuracy.Should().BeGreaterThanOrEqualTo(0.70,
			$"Accuracy must be >= 70%.\n{reportBuilder}");
	}

	private static List<SpellingTestCase> LoadAllTestCases()
	{
		var allCases = new List<SpellingTestCase>();
		var suites = JsonLoader.Load<SpellingTestSuite>("TestCases/uk-UA/SpellChecking");

		foreach (var (suite, _) in suites)
		{
			allCases.AddRange(suite.Cases);
		}
		return allCases;
	}
}