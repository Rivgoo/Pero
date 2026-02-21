using FluentAssertions;
using FluentAssertions.Execution;
using Pero.Abstractions.Models;
using Pero.Kernel.Components;
using Pero.Testing.Shared.Loaders;

namespace Pero.Tests.Kernel.Components;

public class HeuristicCodePreTokenizerTests
{
	private readonly HeuristicCodePreTokenizer _tokenizer;

	public HeuristicCodePreTokenizerTests()
	{
		var inner = new StandardPreTokenizer();
		_tokenizer = new HeuristicCodePreTokenizer(inner);
	}

	public class CodeTestSuite
	{
		public string Description { get; set; } = string.Empty;
		public List<CodeTestCase> Cases { get; set; } = new();
	}

	public class CodeTestCase
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
	[MemberData(nameof(GetCodeTestCases))]
	public void Scan_ShouldDetectCodeBlocks(CodeTestCase testCase, string fileName)
	{
		// Act
		var result = _tokenizer.Scan(testCase.Input).ToList();

		// Assert
		using (new AssertionScope())
		{
			result.Should().HaveSameCount(testCase.Expected,
				because: $"File '{fileName}', Case '{testCase.Name}': Expected {testCase.Expected.Count} fragments but found {result.Count}.\nFound: {string.Join(", ", result.Select(f => $"[{f.Type}] '{f.Text}'"))}");

			for (int i = 0; i < result.Count; i++)
			{
				var actual = result[i];
				var expected = testCase.Expected[i];

				Enum.TryParse<FragmentType>(expected.Type, true, out var expectedType)
					.Should().BeTrue($"'{expected.Type}' must be a valid FragmentType.");

				actual.Type.Should().Be(expectedType, $"Fragment {i} type mismatch in '{testCase.Name}'");
				actual.Text.Should().Be(expected.Text, $"Fragment {i} text mismatch in '{testCase.Name}'");
				actual.Start.Should().Be(expected.Start, $"Fragment {i} start mismatch in '{testCase.Name}'");
				actual.End.Should().Be(expected.End, $"Fragment {i} end mismatch in '{testCase.Name}'");
			}
		}
	}

	public static IEnumerable<object[]> GetCodeTestCases()
	{
		var suites = JsonLoader.Load<CodeTestSuite>("TestCases/PreTokenization");

		foreach (var (suite, fileName) in suites)
			foreach (var testCase in suite.Cases)
				yield return new object[] { testCase, fileName };
	}
}