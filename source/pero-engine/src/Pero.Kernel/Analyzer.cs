using Pero.Abstractions.Models;
using Pero.Kernel.Pipeline;
using Pero.Kernel.Registry;

namespace Pero.Kernel;

/// <summary>
/// Provides a high-level facade for the text analysis engine.
/// This is the primary entry point for external consumers like the WasmHost.
/// </summary>
public class Analyzer
{
	private readonly LanguageRegistry _registry;

	public Analyzer(LanguageRegistry registry)
	{
		_registry = registry;
	}

	/// <summary>
	/// Analyzes a text using the appropriate language module.
	/// </summary>
	public IReadOnlyList<TextIssue> Analyze(string text, string languageCode)
	{
		if (string.IsNullOrEmpty(text))
			return Array.Empty<TextIssue>();

		var module = _registry.GetByCode(languageCode);
		var pipeline = new AnalysisPipeline(module);
		var result = pipeline.Run(text);

		return result.Issues;
	}
}