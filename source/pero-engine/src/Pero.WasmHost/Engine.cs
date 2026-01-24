using System.Diagnostics;
using System.Runtime.InteropServices.JavaScript;
using System.Text.Json;
using Pero.Contracts;
using Pero.Core;

namespace Pero.WasmHost;

public partial class Engine
{
	private static readonly Analyzer _analyzer = new();

	[JSExport]
	public static string Process(string jsonRequest)
	{
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

			var issues = _analyzer.Analyze(request.Text);

			var response = new AnalysisResponse
			{
				RequestId = request.RequestId,
				IsSuccess = true,
				Issues = issues
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