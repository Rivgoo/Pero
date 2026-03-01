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
	private readonly ITestOutputHelper output;
	private readonly AnalysisPipeline pipeline;

	public UkrainianStressTests(ITestOutputHelper output)
	{
		this.output = output;

		pipeline = new AnalysisPipeline(new UkrainianLanguageModule(), enableTelemetry: true);
	}

	[Fact]
	public void Run_ShouldProcessLargeTextWithoutExceptionsAndReportMetrics()
	{
		var text = LoadStressText();
		var stopwatch = Stopwatch.StartNew();

		var result = pipeline.Run(text, enableTelemetry: true);

		stopwatch.Stop();

		result.Should().NotBeNull();
		result.Document.Should().NotBeNull();
		result.Telemetry.Should().NotBeNull();

		ReportMetrics(text.Length, result, stopwatch.Elapsed);
	}

	private void ReportMetrics(int textLength, AnalysisResult result, TimeSpan elapsed)
	{
		var sentencesCount = result.Document!.Sentences.Count;
		var tokensCount = result.Document.Sentences.Sum(s => s.Tokens.Count);
		var wordsCount = result.Document.Sentences.Sum(s => s.Tokens.Count(t => t.Type == TokenType.Word));
		var issuesCount = result.Issues.Count;

		var charsPerSecond = textLength / elapsed.TotalSeconds;
		var wordsPerSecond = wordsCount / elapsed.TotalSeconds;

		output.WriteLine("=== PERFORMANCE METRICS ===");
		output.WriteLine($"Total Time:      {elapsed.TotalMilliseconds:N0} ms");
		output.WriteLine($"Text Length:     {textLength:N0} chars");
		output.WriteLine($"Words:           {wordsCount:N0}");
		output.WriteLine($"Tokens:          {tokensCount:N0}");
		output.WriteLine($"Sentences:       {sentencesCount:N0}");
		output.WriteLine($"Issues Found:    {issuesCount:N0}");

		output.WriteLine("\n--- PIPELINE STAGES (Execution Order) ---");
		var stages = result.Telemetry!.Metrics
			.Where(k => k.Key.StartsWith("Stage."))
			.ToList();

		foreach (var kvp in stages)
		{
			output.WriteLine($"{kvp.Key.Replace("Stage.", ""),-20}: {kvp.Value,8:F2} ms");
		}

		output.WriteLine("\n--- SPELL CHECK DETAILS (Slowest First) ---");
		var spellCheckMetrics = result.Telemetry.Metrics
			.Where(k => k.Key.StartsWith("SpellCheck."))
			.OrderByDescending(x => x.Value)
			.ToList();

		foreach (var kvp in spellCheckMetrics)
		{
			output.WriteLine($"{kvp.Key.Replace("SpellCheck.", ""),-25}: {kvp.Value,8:F2} ms");
		}

		output.WriteLine("\n--- GRAMMAR & SPELLING ANALYZERS (Slowest First) ---");
		var analyzerMetrics = result.Telemetry.Metrics
			.Where(k => k.Key.StartsWith("Analyzer."))
			.OrderByDescending(x => x.Value)
			.ToList();

		if (analyzerMetrics.Count > 0)
		{
			foreach (var kvp in analyzerMetrics)
			{
				output.WriteLine($"{kvp.Key.Replace("Analyzer.", ""),-35}: {kvp.Value,8:F2} ms");
			}
		}
		else
		{
			output.WriteLine("No analyzers executed or no telemetry collected.");
		}

		output.WriteLine($"\n[Total Internally Tracked Pipeline Time: {result.Telemetry.Metrics.GetValueOrDefault("Pipeline.Total", 0):F2} ms]");

		output.WriteLine("\n--- SPEED ---");
		output.WriteLine($"Chars/sec:       {charsPerSecond:N0}");
		output.WriteLine($"Words/sec:       {wordsPerSecond:N0}");

		output.WriteLine("\n--- ISSUES BREAKDOWN ---");
		if (result.Issues.Count > 0)
		{
			var issuesByRule = result.Issues
				.GroupBy(i => i.RuleId)
				.OrderByDescending(g => g.Count());

			foreach (var group in issuesByRule)
			{
				output.WriteLine($"{group.Key,-35}: {group.Count(),5:N0} issues");
			}
		}
		else
		{
			output.WriteLine("No issues found in the text.");
		}

		output.WriteLine("===========================");
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