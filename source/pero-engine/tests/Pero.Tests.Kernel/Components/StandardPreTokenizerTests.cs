using FluentAssertions;
using FluentAssertions.Execution;
using Pero.Abstractions.Models;
using Pero.Kernel.Components;
using Pero.Kernel.Configuration;
using Pero.Testing.Shared.Loaders;

namespace Pero.Tests.Kernel.Components;

public class StandardPreTokenizerTests
{
	private readonly StandardPreTokenizer _preTokenizer = new(PreTokenizerConfig.CreateDefault());

	public class PreTokenizationTestSuite
	{
		public string Description { get; set; } = string.Empty;
		public List<PreTokenizationTestCase> Cases { get; set; } = new();
	}

	public class PreTokenizationTestCase
	{
		public string Name { get; set; } = string.Empty;
		public string Input { get; set; } = string.Empty;
		public List<ExpectedFragment> Expected { get; set; } = new();
	}

	public class ExpectedFragment
	{
		public string Type { get; set; } = string.Empty;
		public string Text { get; set; } = string.Empty;
		public int Start { get; set; }
		public int End { get; set; }
	}

	[Theory]
	[MemberData(nameof(GetTestCases))]
	public void Scan_ShouldCorrectlyIdentifyAndPositionFragments(PreTokenizationTestCase testCase, string fileName)
	{
		// Act
		var result = _preTokenizer.Scan(testCase.Input).ToList();

		// Assert
		using (new AssertionScope())
		{
			result.Should().HaveSameCount(testCase.Expected,
				because: $"File '{fileName}', Case '{testCase.Name}': Expected {testCase.Expected.Count} fragments but found {result.Count}.");

			for (int i = 0; i < result.Count; i++)
			{
				var actual = result[i];
				var expected = testCase.Expected[i];

				Enum.TryParse<FragmentType>(expected.Type, true, out var expectedType)
					.Should().BeTrue($"'{expected.Type}' must be a valid FragmentType enum value.");

				actual.Type.Should().Be(expectedType, $"Fragment {i} type should match in '{testCase.Name}'");
				actual.Text.Should().Be(expected.Text, $"Fragment {i} text should match in '{testCase.Name}'");
				actual.Start.Should().Be(expected.Start, $"Fragment {i} start position should match in '{testCase.Name}'");
				actual.End.Should().Be(expected.End, $"Fragment {i} end position should match in '{testCase.Name}'");
			}
		}
	}

	public static IEnumerable<object[]> GetTestCases()
	{
		var suites = JsonLoader.Load<PreTokenizationTestSuite>("TestCases/Kernel/PreTokenization/Standard");

		foreach (var (suite, fileName) in suites)
		{
			foreach (var testCase in suite.Cases)
			{
				yield return new object[] { testCase, fileName };
			}
		}
	}
}