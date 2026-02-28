using Pero.Abstractions.Models;
using Pero.Kernel.Pipeline;
using Pero.Kernel.Registry;

namespace Pero.Kernel;

public class Analyzer
{
	private readonly LanguageRegistry registry;

	public Analyzer(LanguageRegistry registry)
	{
		this.registry = registry;
	}

	public IReadOnlyList<TextIssue> Analyze(string text, string languageCode, bool enableTelemetry = false)
	{
		if (string.IsNullOrEmpty(text))
			return new List<TextIssue>();

		var module = registry.GetByCode(languageCode);
		var pipeline = new AnalysisPipeline(module, enableTelemetry);
		var result = pipeline.Run(text, enableTelemetry);

		return result.Issues;
	}
}