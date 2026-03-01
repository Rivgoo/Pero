using Pero.Abstractions.Transport;
using Pero.Kernel;
using Pero.Kernel.Registry;
using Pero.Languages.Uk_UA;
using System.Diagnostics;
using System.Runtime.InteropServices.JavaScript;
using System.Text.Json;

namespace Pero.WasmHost;

public partial class Engine
{
	private static Analyzer _analyzer = null!;
	private static bool _isInitialized = false;

	private static void Initialize()
	{
		var registry = new LanguageRegistry();

		registry.Register(new UkrainianLanguageModule());

		_analyzer = new Analyzer(registry);
		_isInitialized = true;
	}

	[JSExport]
	public static string Process(string jsonRequest)
	{
		if (_isInitialized == false)
		{
			Initialize();
		}

		var stopwatch = Stopwatch.StartNew();

		try
		{
			if (string.IsNullOrWhiteSpace(jsonRequest))
			{
				return CreateErrorResponse("Empty request");
			}

			var request = JsonSerializer.Deserialize(
				jsonRequest,
				PeroJsonContext.Default.AnalysisRequest
			);

			if (request == null)
			{
				return CreateErrorResponse("Invalid JSON");
			}

			var issues = _analyzer.Analyze(
				request.Text,
				request.LanguageCode,
				enableTelemetry: true,
				disabledRules: request.DisabledRules
				);

			var response = new AnalysisResponse
			{
				RequestId = request.RequestId,
				IsSuccess = true,
				Issues = issues.ToList()
			};

			stopwatch.Stop();
			response.ProcessingTimeMs = stopwatch.Elapsed.TotalMilliseconds;

			return JsonSerializer.Serialize(response, PeroJsonContext.Default.AnalysisResponse);
		}
		catch (Exception ex)
		{
			return CreateErrorResponse(ex.Message);
		}
	}

	private static string CreateErrorResponse(string message)
	{
		var error = new AnalysisResponse
		{
			RequestId = "error",
			IsSuccess = false,
			ErrorMessage = message
		};
		return JsonSerializer.Serialize(error, PeroJsonContext.Default.AnalysisResponse);
	}
}