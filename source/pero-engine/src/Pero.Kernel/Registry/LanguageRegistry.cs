using Pero.Abstractions.Contracts;

namespace Pero.Kernel.Registry;

/// <summary>
/// A simple, AOT-friendly registry for managing available language modules.
/// Acts as a service locator for the analysis engine.
/// </summary>
public class LanguageRegistry
{
	private readonly Dictionary<string, ILanguageModule> _modules = new();

	/// <summary>
	/// Registers a language module, making it available for analysis.
	/// </summary>
	public void Register(ILanguageModule module)
	{
		_modules[module.LanguageCode] = module;
	}

	/// <summary>
	/// Retrieves a registered language module by its code.
	/// </summary>
	/// <exception cref="NotSupportedException">
	/// Thrown if no module is registered for the given language code.
	/// </exception>
	public ILanguageModule GetByCode(string languageCode)
	{
		if (_modules.TryGetValue(languageCode, out var module))
		{
			return module;
		}

		throw new NotSupportedException($"Language '{languageCode}' is not supported.");
	}
}