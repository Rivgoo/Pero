using Pero.Languages.Uk_UA.Dictionaries.Ngrams;
using System.Reflection;

namespace Pero.Languages.Uk_UA.Resources;

public static class NgramProvider
{
	private const string _resourceName = "Pero.Languages.Uk_UA.Resources.uk_UA.perongram";

	private static NgramLanguageModel? _cachedModel;
	private static readonly object _lock = new();

	public static NgramLanguageModel GetModel()
	{
		if (_cachedModel != null) return _cachedModel;

		lock (_lock)
		{
			if (_cachedModel != null) return _cachedModel;

			var model = new NgramLanguageModel();
			var assembly = Assembly.GetExecutingAssembly();

			using var stream = assembly.GetManifestResourceStream(_resourceName);

			if (stream != null)
			{
				try
				{
					model.Load(stream);
				}
				catch
				{
					// In case of corrupted resource, we fallback to empty model rather than crashing app start
					// In production this should be logged, but for library code we prefer stability.
				}
			}

			_cachedModel = model;
			return _cachedModel;
		}
	}
}