using FluentAssertions;
using FluentAssertions.Execution;
using Pero.Abstractions.Models;
using Pero.Testing.Shared.Data;

namespace Pero.Testing.Shared.Assertions;

/// <summary>
/// Static helper class containing reusable assertion logic for TextIssues.
/// </summary>
public static class IssueVerifier
{
	public static void Verify(
		string input,
		IReadOnlyList<TextIssue> actualIssues,
		List<ExpectedIssue> expectedIssues)
	{
		using (new AssertionScope())
		{
			actualIssues.Should().HaveSameCount(expectedIssues,
				because: $"Input: '{input}'\nExpected {expectedIssues.Count} issues but found {actualIssues.Count}.\nFound: {string.Join(", ", actualIssues.Select(i => i.RuleId))}");

			// Sort both lists by position to ensure alignment
			var sortedActual = actualIssues.OrderBy(i => i.Start).ToList();
			var sortedExpected = expectedIssues.OrderBy(i => i.Start).ToList();

			for (int i = 0; i < sortedActual.Count; i++)
			{
				var actual = sortedActual[i];
				var expected = sortedExpected[i];

				actual.Original.Should().Be(expected.Original, "Original text fragment should match");
				actual.Start.Should().Be(expected.Start, "Start position should match");
				actual.End.Should().Be(expected.End, "End position should match");

				if (expected.Suggestions != null && expected.Suggestions.Count != 0)
				{
					actual.Suggestions.Should().BeEquivalentTo(expected.Suggestions, "Suggestions list should match");
				}

				if (expected.Args != null)
				{
					actual.MessageArgs.Should().NotBeNull();
					actual.MessageArgs.Should().Contain(expected.Args, "Message arguments should match");
				}
			}
		}
	}
}