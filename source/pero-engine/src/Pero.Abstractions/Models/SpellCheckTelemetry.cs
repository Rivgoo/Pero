namespace Pero.Abstractions.Models;

/// <summary>
/// Contains highly detailed execution times for the internal stages of the spell checker.
/// </summary>
public class SpellCheckTelemetry
{
	public double SessionCacheInitMs { get; set; }
	public double NonUkrainianCheckMs { get; set; }
	public double StringNormalizationMs { get; set; }
	public double HeuristicsGenerationMs { get; set; }
	public double HeuristicsDictionaryLookupMs { get; set; }
	public double SymSpellMs { get; set; }
	public double FuzzyMatcherMs { get; set; }
	public double ContextRankingMs { get; set; }
	public double SuggestionFormattingMs { get; set; }
}