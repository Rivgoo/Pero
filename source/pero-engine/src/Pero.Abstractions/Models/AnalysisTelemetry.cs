namespace Pero.Abstractions.Models;

/// <summary>
/// Contains execution time metrics for the various stages of the analysis pipeline.
/// Populated only when telemetry is explicitly enabled.
/// </summary>
public class AnalysisTelemetry
{
	public double CleaningMs { get; set; }
	public double PreTokenizationMs { get; set; }
	public double TokenizationMs { get; set; }
	public double SegmentationMs { get; set; }
	public double MorphologyMs { get; set; }
	public double SpellCheckMs { get; set; }
	public double GrammarRulesMs { get; set; }
	public double TotalMs { get; set; }

	public SpellCheckTelemetry? SpellCheckDetails { get; set; }
}