using System.Text.Json;

namespace Pero.Kernel.Utils;

public static class FileSystemJsonLoader
{
	private static readonly JsonSerializerOptions _options = new()
	{
		PropertyNameCaseInsensitive = true,
		ReadCommentHandling = JsonCommentHandling.Skip,
		AllowTrailingCommas = true
	};

	public static T Load<T>(string relativePath)
	{
		var basePath = AppContext.BaseDirectory;
		var fullPath = Path.Combine(basePath, relativePath);

		if (!File.Exists(fullPath))
		{
			throw new FileNotFoundException($"Configuration file not found at: {fullPath}");
		}

		var json = File.ReadAllText(fullPath);

		return JsonSerializer.Deserialize<T>(json, _options)
			   ?? throw new InvalidOperationException($"Failed to deserialize file: {fullPath}");
	}
}