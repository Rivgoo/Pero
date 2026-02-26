using System.Text.Json.Serialization;
using Pero.Abstractions.Transport;

namespace Pero.WasmHost
{
	[JsonSourceGenerationOptions(
		PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
		UseStringEnumConverter = true,
		WriteIndented = false)]
	[JsonSerializable(typeof(AnalysisRequest))]
	[JsonSerializable(typeof(AnalysisResponse))]
	[JsonSerializable(typeof(Dictionary<string, string>))]
	internal partial class PeroJsonContext : JsonSerializerContext
	{
	}
}