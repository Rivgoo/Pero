using System.Diagnostics;
using System.Runtime.InteropServices.JavaScript;
using System.Text.Json;
using Pero.Abstractions.Constants;
using Pero.Abstractions.Transport;
using Pero.Kernel;
using Pero.Kernel.Registry;

namespace Pero.WasmHost;

public partial class Engine
{
	private static Analyzer _analyzer = null!;
	private static bool _isInitialized = false;

	private static void Initialize()
	{
		var registry = new LanguageRegistry();
		_analyzer = new Analyzer(registry);

		_isInitialized = true;
	}

	[JSExport]
	public static string Process(string jsonRequest)
	{
		if(_isInitialized == false)
			Initialize();

		var stopwatch = Stopwatch.StartNew();

		try
		{
			if (string.IsNullOrWhiteSpace(jsonRequest))
				return CreateErrorResponse("Empty request");

			var request = JsonSerializer.Deserialize(
				jsonRequest,
				PeroJsonContext.Default.AnalysisRequest
			);

			if (request == null)
				return CreateErrorResponse("Invalid JSON");

			var issues = _analyzer.Analyze(request.Text, LanguageCodes.Ukrainian);

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
			IsSuccess = false
		};
		return JsonSerializer.Serialize(error, PeroJsonContext.Default.AnalysisResponse);
	}
}