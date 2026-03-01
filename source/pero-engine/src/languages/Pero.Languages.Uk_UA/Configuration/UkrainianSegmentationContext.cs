using System.Text.Json.Serialization;

namespace Pero.Languages.Uk_UA.Configuration;

[JsonSerializable(typeof(SegmentationProfileDto))]
[JsonSourceGenerationOptions(
		PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
		UseStringEnumConverter = true,
		WriteIndented = false)]
internal partial class UkrainianSegmentationContext : JsonSerializerContext
{
}