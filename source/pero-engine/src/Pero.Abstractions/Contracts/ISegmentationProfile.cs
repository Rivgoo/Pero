namespace Pero.Abstractions.Contracts;

public interface ISegmentationProfile
{
	IReadOnlySet<string> Terminators { get; }
	IReadOnlySet<string> ClosingQuotes { get; }
	IReadOnlySet<string> StructuralAbbreviations { get; }
	IReadOnlySet<string> TitleAbbreviations { get; }
	IReadOnlySet<string> UnitAbbreviations { get; }
}