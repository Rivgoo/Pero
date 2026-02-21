using System.Text.Json;
using Pero.Testing.Shared.Data;

namespace Pero.Testing.Shared.Loaders;

public static class TestSuiteLoader
{
	private static readonly JsonSerializerOptions _options = new()
	{
		PropertyNameCaseInsensitive = true,
		ReadCommentHandling = JsonCommentHandling.Skip, // Allow comments in JSON!
		AllowTrailingCommas = true
	};

	public static IEnumerable<object[]> Load(string relativePath)
	{
		var directory = ResolveDirectory(relativePath);
		var files = Directory.GetFiles(directory, "*.json", SearchOption.AllDirectories);

		foreach (var file in files)
		{
			var json = File.ReadAllText(file);
			TestSuite? suite;

			try
			{
				suite = JsonSerializer.Deserialize<TestSuite>(json, _options);
			}
			catch (JsonException ex)
			{
				throw new InvalidOperationException($"Failed to parse test file: {file}. Error: {ex.Message}");
			}

			if (suite == null || suite.Cases == null) continue;

			foreach (var testCase in suite.Cases)
			{
				ValidateTestCase(testCase, file);

				// Yield: [RuleId (or SuiteId), TestCase, FileName]
				// We fallback to Suite Description if RuleId is missing
				var testName = !string.IsNullOrEmpty(suite.RuleId) ? suite.RuleId : suite.Description;
				yield return new object[] { testName, testCase, Path.GetFileName(file) };
			}
		}
	}

	private static string ResolveDirectory(string relativePath)
	{
		var baseDir = AppContext.BaseDirectory;
		var fullPath = Path.GetFullPath(Path.Combine(baseDir, relativePath));

		if (!Directory.Exists(fullPath))
		{
			// Try walking up for local dev environments vs CI
			var candidate = Path.GetFullPath(Path.Combine(baseDir, "../../../", relativePath));
			if (Directory.Exists(candidate)) return candidate;

			throw new DirectoryNotFoundException($"Test data directory not found at {fullPath} or {candidate}");
		}

		return fullPath;
	}

	private static void ValidateTestCase(TestCase testCase, string fileName)
	{
		foreach (var issue in testCase.Issues)
		{
			if (issue.Start < 0 || issue.End <= issue.Start)
			{
				throw new InvalidDataException($"Invalid indices in file '{fileName}': {issue.Original} ({issue.Start}-{issue.End})");
			}

			// Validate that length matches original text length
			if (issue.Original.Length != (issue.End - issue.Start))
			{
				throw new InvalidDataException($"Length mismatch in file '{fileName}'. Text '{issue.Original}' length is {issue.Original.Length}, but indices cover {issue.End - issue.Start}.");
			}
		}
	}
}