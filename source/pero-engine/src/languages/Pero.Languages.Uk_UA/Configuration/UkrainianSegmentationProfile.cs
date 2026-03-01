using System.Reflection;
using System.Text.Json;
using Pero.Abstractions.Contracts;

namespace Pero.Languages.Uk_UA.Configuration;

public class UkrainianSegmentationProfile : ISegmentationProfile
{
	public IReadOnlySet<string> Terminators { get; }
	public IReadOnlySet<string> ClosingQuotes { get; }
	public IReadOnlySet<string> StructuralAbbreviations { get; }
	public IReadOnlySet<string> TitleAbbreviations { get; }
	public IReadOnlySet<string> UnitAbbreviations { get; }

	public UkrainianSegmentationProfile()
	{
		var assembly = Assembly.GetExecutingAssembly();
		using var stream = assembly.GetManifestResourceStream("Pero.Languages.Uk_UA.Resources.uk_UA_segmentation.json");

		if (stream == null)
		{
			throw new FileNotFoundException("Segmentation profile resource 'uk_UA_segmentation.json' not found.");
		}

		var dto = JsonSerializer.Deserialize(
			stream,
			UkrainianSegmentationContext.Default.SegmentationProfileDto
		) ?? throw new InvalidOperationException("Failed to parse segmentation profile.");

		Terminators = new HashSet<string>(dto.Terminators, StringComparer.Ordinal);
		ClosingQuotes = new HashSet<string>(dto.ClosingQuotes, StringComparer.Ordinal);
		StructuralAbbreviations = new HashSet<string>(dto.StructuralAbbreviations, StringComparer.OrdinalIgnoreCase);
		TitleAbbreviations = new HashSet<string>(dto.TitleAbbreviations, StringComparer.OrdinalIgnoreCase);
		UnitAbbreviations = new HashSet<string>(dto.UnitAbbreviations, StringComparer.OrdinalIgnoreCase);
	}
}

internal class SegmentationProfileDto
{
	public List<string> Terminators { get; set; } = new();
	public List<string> ClosingQuotes { get; set; } = new();
	public List<string> StructuralAbbreviations { get; set; } = new();
	public List<string> TitleAbbreviations { get; set; } = new();
	public List<string> UnitAbbreviations { get; set; } = new();
}