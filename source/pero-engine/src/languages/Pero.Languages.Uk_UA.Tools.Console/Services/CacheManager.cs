using System.Text.Json;

namespace Pero.Languages.Uk_UA.Tools.Console.Services;

public class CacheManager
{
	private readonly string _cacheDirectory;

	public CacheManager(string baseDirectory)
	{
		_cacheDirectory = Path.Combine(baseDirectory, ".cache");
		Directory.CreateDirectory(_cacheDirectory);
	}

	public void Save<T>(string key, T data)
	{
		string path = Path.Combine(_cacheDirectory, $"{key}.json");
		using var stream = File.Create(path);
		JsonSerializer.Serialize(stream, data);
	}

	public T? Load<T>(string key)
	{
		string path = Path.Combine(_cacheDirectory, $"{key}.json");
		if (!File.Exists(path)) return default;

		using var stream = File.OpenRead(path);
		return JsonSerializer.Deserialize<T>(stream);
	}

	public bool Exists(string key)
	{
		return File.Exists(Path.Combine(_cacheDirectory, $"{key}.json"));
	}
}