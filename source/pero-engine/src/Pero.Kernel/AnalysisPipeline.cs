using System.Diagnostics;
using Pero.Abstractions.Contracts;
using Pero.Abstractions.Models;

namespace Pero.Kernel.Pipeline;

/// <summary>
/// Orchestrates the multi-stage text analysis process for a specific language.
/// This class is designed to be stateless and created per analysis request.
/// </summary>
public class AnalysisPipeline
{
	private readonly ILanguageModule _module;

	public AnalysisPipeline(ILanguageModule module)
	{
		_module = module;
	}

	/// <summary>
	/// Executes the full analysis pipeline on the given text.
	/// </summary>
	/// <param name="rawText">The text to analyze.</param>
	/// <param name="enableTelemetry">If true, captures precise execution times for each pipeline stage.</param>
	public AnalysisResult Run(string rawText, bool enableTelemetry = false)
	{
		AnalysisTelemetry? telemetry = enableTelemetry ? new AnalysisTelemetry() : null;
		long totalStart = enableTelemetry ? Stopwatch.GetTimestamp() : 0;
		long stepStart = 0;

		var cleaner = _module.CreateTextCleaner();
		var preTokenizer = _module.CreatePreTokenizer();
		var tokenizer = _module.CreateTokenizer();
		var segmenter = _module.CreateSentenceSegmenter();
		var morphAnalyzer = _module.CreateMorphologyAnalyzer();
		var spellChecker = _module.CreateSpellChecker();
		var rules = _module.GetRules().ToList();

		spellChecker.EnableTelemetry = true;

		// 1. Cleaning
		if (enableTelemetry) stepStart = Stopwatch.GetTimestamp();
		var cleanedText = cleaner.Clean(rawText);
		if (enableTelemetry) telemetry!.CleaningMs = GetElapsedMs(stepStart);

		// 2. Pre-Tokenization & Tokenization
		List<Token> allTokens;
		if (enableTelemetry)
		{
			stepStart = Stopwatch.GetTimestamp();
			var fragments = preTokenizer.Scan(cleanedText).ToList();
			telemetry!.PreTokenizationMs = GetElapsedMs(stepStart);

			stepStart = Stopwatch.GetTimestamp();
			allTokens = ProcessFragments(fragments, tokenizer);
			telemetry.TokenizationMs = GetElapsedMs(stepStart);
		}
		else
		{
			var fragments = preTokenizer.Scan(cleanedText);
			allTokens = ProcessFragments(fragments, tokenizer);
		}

		// 3. Segmentation
		if (enableTelemetry) stepStart = Stopwatch.GetTimestamp();
		var sentences = segmenter.Segment(allTokens).ToList();
		if (enableTelemetry) telemetry!.SegmentationMs = GetElapsedMs(stepStart);

		var document = new AnalyzedDocument(rawText, sentences);

		// 4. Morphology
		if (enableTelemetry) stepStart = Stopwatch.GetTimestamp();
		foreach (var sentence in sentences)
		{
			morphAnalyzer.Enrich(sentence);
		}
		if (enableTelemetry) telemetry!.MorphologyMs = GetElapsedMs(stepStart);

		var allIssues = new List<TextIssue>();

		// 5. Spell Checking
		if (enableTelemetry) stepStart = Stopwatch.GetTimestamp();
		allIssues.AddRange(spellChecker.Check(document));
		if (enableTelemetry)
		{
			telemetry!.SpellCheckMs = GetElapsedMs(stepStart);

			telemetry.SpellCheckDetails = spellChecker.LastTelemetry;
		}

		// 6. Grammar Rules
		if (enableTelemetry) stepStart = Stopwatch.GetTimestamp();
		foreach (var sentence in sentences)
		{
			foreach (var rule in rules)
			{
				allIssues.AddRange(rule.Check(sentence));
			}
		}
		if (enableTelemetry) telemetry!.GrammarRulesMs = GetElapsedMs(stepStart);

		if (enableTelemetry) telemetry!.TotalMs = GetElapsedMs(totalStart);

		return new AnalysisResult(document, allIssues, telemetry);
	}

	private static List<Token> ProcessFragments(IEnumerable<TextFragment> fragments, ITokenizer tokenizer)
	{
		var tokens = new List<Token>();
		foreach (var fragment in fragments)
		{
			if (fragment.Type == FragmentType.Raw)
			{
				tokens.AddRange(tokenizer.Tokenize(fragment));
			}
			else
			{
				tokens.Add(new Token(
					text: fragment.Text,
					normalizedText: fragment.Text,
					type: FragmentTokenMapper.Map(fragment.Type),
					start: fragment.Start,
					end: fragment.End
				));
			}
		}
		return tokens;
	}

	private static double GetElapsedMs(long startTimestamp)
	{
		return (Stopwatch.GetTimestamp() - startTimestamp) * 1000.0 / Stopwatch.Frequency;
	}
}