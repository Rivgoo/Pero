using FluentAssertions;
using Pero.Kernel.Components;
using Pero.Testing.Shared.Loaders;

namespace Pero.Tests.Kernel.Components;

public class StandardTextCleanerTests
{
	private readonly StandardTextCleaner _cleaner = new();

	[Theory]
	[MemberData(nameof(GetTestCases))]
	public void Clean_ShouldMatchExpectedOutput(string input, string expected, string caseName)
	{
		var result = _cleaner.Clean(input);

		result.Should().Be(expected,
			because: $"Case '{caseName}' failed.");
	}

	public static IEnumerable<object[]> GetTestCases()
	{
		return PtfLoader.Load("TestCases/Cleaning");
	}
}