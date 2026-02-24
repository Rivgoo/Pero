using System.Diagnostics;
using FluentAssertions;
using Pero.Abstractions.Models;
using Pero.Kernel;
using Pero.Kernel.Pipeline;
using Pero.Languages.Uk_UA;
using Xunit.Abstractions;

namespace Pero.Tests.Languages.Uk_UA.Pipeline;

public class UkrainianStressTests
{
	private readonly ITestOutputHelper _output;
	private readonly AnalysisPipeline _pipeline;

	public UkrainianStressTests(ITestOutputHelper output)
	{
		_output = output;
		_pipeline = new AnalysisPipeline(new UkrainianLanguageModule());
	}

	[Fact]
	public void Run_ShouldProcessLargeTextWithoutExceptionsAndReportMetrics()
	{
		var text = LoadStressText();

		var stopwatch = Stopwatch.StartNew();

		var result = _pipeline.Run(text, enableTelemetry: true);

		stopwatch.Stop();

		result.Should().NotBeNull();
		result.Document.Should().NotBeNull();
		result.Telemetry.Should().NotBeNull();

		ReportMetrics(text.Length, result, stopwatch.Elapsed);
	}

	private void ReportMetrics(int textLength, AnalysisResult result, TimeSpan elapsed)
	{
		var sentencesCount = result.Document.Sentences.Count;
		var tokensCount = result.Document.Sentences.Sum(s => s.Tokens.Count);
		var wordsCount = result.Document.Sentences.Sum(s => s.Tokens.Count(t => t.Type == TokenType.Word));
		var issuesCount = result.Issues.Count;

		var charsPerSecond = textLength / elapsed.TotalSeconds;
		var wordsPerSecond = wordsCount / elapsed.TotalSeconds;

		_output.WriteLine("=== PERFORMANCE METRICS ===");
		_output.WriteLine($"Total Time:      {elapsed.TotalMilliseconds:N0} ms");
		_output.WriteLine($"Text Length:     {textLength:N0} chars");
		_output.WriteLine($"Words:           {wordsCount:N0}");
		_output.WriteLine($"Tokens:          {tokensCount:N0}");
		_output.WriteLine($"Sentences:       {sentencesCount:N0}");
		_output.WriteLine($"Issues Found:    {issuesCount:N0}");

		_output.WriteLine("\n--- STAGE TELEMETRY ---");
		_output.WriteLine($"1. Cleaning:       {result.Telemetry!.CleaningMs:F2} ms");
		_output.WriteLine($"2. Pre-Tokenize:   {result.Telemetry.PreTokenizationMs:F2} ms");
		_output.WriteLine($"3. Tokenize:       {result.Telemetry.TokenizationMs:F2} ms");
		_output.WriteLine($"4. Segmentation:   {result.Telemetry.SegmentationMs:F2} ms");
		_output.WriteLine($"5. Morphology:     {result.Telemetry.MorphologyMs:F2} ms");
		_output.WriteLine($"6. Spell Check:    {result.Telemetry.SpellCheckMs:F2} ms");
		_output.WriteLine($"7. Grammar Rules:  {result.Telemetry.GrammarRulesMs:F2} ms");
		_output.WriteLine($"   [Pipeline Internal Total: {result.Telemetry.TotalMs:F2} ms]");

		if (result.Telemetry.SpellCheckDetails != null)
		{
			var sc = result.Telemetry.SpellCheckDetails;
			_output.WriteLine("\n--- SPELL CHECK DETAILS ---");
			_output.WriteLine($"Session Cache Init: {sc.SessionCacheInitMs:F2} ms");
			_output.WriteLine($"Non-UA Check:       {sc.NonUkrainianCheckMs:F2} ms");
			_output.WriteLine($"String Norm:        {sc.StringNormalizationMs:F2} ms");
			_output.WriteLine($"Heuristics Gen:     {sc.HeuristicsGenerationMs:F2} ms");
			_output.WriteLine($"Heuristics Lookup:  {sc.HeuristicsDictionaryLookupMs:F2} ms");
			_output.WriteLine($"Virtual SymSpell:   {sc.SymSpellMs:F2} ms");
			_output.WriteLine($"Fuzzy Matcher:      {sc.FuzzyMatcherMs:F2} ms");
			_output.WriteLine($"Context Ranking:    {sc.ContextRankingMs:F2} ms");
			_output.WriteLine($"Suggestion Format:  {sc.SuggestionFormattingMs:F2} ms");
		}

		_output.WriteLine("\n--- SPEED ---");
		_output.WriteLine($"Chars/sec:       {charsPerSecond:N0}");
		_output.WriteLine($"Words/sec:       {wordsPerSecond:N0}");
		_output.WriteLine("===========================");
	}

	private static string LoadStressText()
	{
		var relativePath = Path.Combine("TestCases", "uk-UA", "Stress", "EndToEndStressTest.txt");
		var baseDir = AppContext.BaseDirectory;

		var primaryPath = Path.GetFullPath(Path.Combine(baseDir, relativePath));
		if (File.Exists(primaryPath)) return File.ReadAllText(primaryPath);

		return "Резервний текст. Файл для стрес-тесту не знайдено. Чекаю на наступний етап.";
	}
}