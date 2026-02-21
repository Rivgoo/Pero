using System.Text.Json;

namespace Pero.Testing.Shared.Loaders;

/// <summary>
/// A universal JSON loader for test data.
/// Handles robust file path resolution and forgiving JSON parsing (allows comments, trailing commas).
/// </summary>
public static class JsonLoader
{
	private static readonly JsonSerializerOptions _options = new()
	{
		PropertyNameCaseInsensitive = true,
		ReadCommentHandling = JsonCommentHandling.Skip,
		AllowTrailingCommas = true
	};

	/// <summary>
	/// Loads and deserializes all JSON files in the specified directory into the target type.
	/// </summary>
	/// <typeparam name="TSuite">The root DTO type representing the JSON file structure.</typeparam>
	/// <param name="relativePath">Path relative to the test assembly output directory.</param>
	/// <returns>An enumeration of deserialized objects and their source file names.</returns>
	public static IEnumerable<(TSuite Suite, string FileName)> Load<TSuite>(string relativePath)
	{
		var directory = ResolveDirectory(relativePath);
		var files = Directory.GetFiles(directory, "*.json", SearchOption.AllDirectories);

		if (files.Length == 0)
		{
			throw new FileNotFoundException($"No .json files found in directory: {directory}");
		}

		foreach (var file in files)
		{
			var fileName = Path.GetFileName(file);
			var json = File.ReadAllText(file);

			TSuite? suite;
			try
			{
				suite = JsonSerializer.Deserialize<TSuite>(json, _options);
			}
			catch (JsonException ex)
			{
				throw new InvalidOperationException($"Failed to parse JSON file '{fileName}'. Error: {ex.Message}");
			}

			if (suite != null)
			{
				yield return (suite, fileName);
			}
		}
	}

	private static string ResolveDirectory(string relativePath)
	{
		var baseDir = AppContext.BaseDirectory;
		var fullPath = Path.GetFullPath(Path.Combine(baseDir, relativePath));

		if (Directory.Exists(fullPath))
		{
			return fullPath;
		}

		var candidate = Path.GetFullPath(Path.Combine(baseDir, "../../../", relativePath));
		if (Directory.Exists(candidate))
		{
			return candidate;
		}

		throw new DirectoryNotFoundException($"Test data directory not found at '{fullPath}'. Ensure CopyToOutputDirectory is enabled for your .json files.");
	}
}