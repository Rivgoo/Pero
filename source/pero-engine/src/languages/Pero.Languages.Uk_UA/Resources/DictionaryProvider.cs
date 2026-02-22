using Pero.Languages.Uk_UA.Dictionaries;
using System.Reflection;

namespace Pero.Languages.Uk_UA.Resources;

public static class DictionaryProvider
{
	private const string _resourceName = "Pero.Languages.Uk_UA.Resources.uk_UA.perodic";

	private static CompiledDictionary? _cachedDictionary;
	private static readonly object _lock = new();

	public static CompiledDictionary GetDictionary()
	{
		if (_cachedDictionary != null) return _cachedDictionary;

		lock (_lock)
		{
			if (_cachedDictionary != null) return _cachedDictionary;

			var assembly = Assembly.GetExecutingAssembly();
			using var stream = assembly.GetManifestResourceStream(_resourceName);

			if (stream == null)
				throw new FileNotFoundException($"Embedded dictionary resource '{_resourceName}' not found.");

			var dictionary = new CompiledDictionary();
			dictionary.Load(stream);

			_cachedDictionary = dictionary;
			return _cachedDictionary;
		}
	}
}