using Pero.Languages.Uk_UA.Tools.Console.Services;
using Pero.Languages.Uk_UA.Tools.Console.UI;
using Pero.Tools.Compiler.Services;
using System.Diagnostics;

namespace Pero.Languages.Uk_UA.Tools.Console.Commands;

public class CompileCommand
{
	private readonly ConsoleInterface ui;
	private readonly FileLocator fileLocator;
	private readonly DictionaryCompilerFacade compiler;
	private readonly CacheManager cacheManager;

	public CompileCommand(ConsoleInterface ui, FileLocator fileLocator, DictionaryCompilerFacade compiler)
	{
		this.ui = ui;
		this.fileLocator = fileLocator;
		this.compiler = compiler;
		this.cacheManager = new CacheManager(fileLocator.BaseDirectory);
	}

	public void Execute()
	{
		ui.ShowHeader("Compile FST Dictionary (with Caching)");

		var textFiles = fileLocator.FindTextFiles();
		if (textFiles.Count == 0)
		{
			ui.ShowError($"No .txt files found in {fileLocator.BaseDirectory}");
			ui.WaitForKey();
			return;
		}
		var selectedFile = textFiles[ui.SelectOption("Select a source file to compile", textFiles.Select(Path.GetFileName).ToList()!)];

		var corpusDir = ui.PromptInput("Enter path to corpus directory for frequency analysis (leave empty to skip)");
		var corpusFiles = fileLocator.GetCorpusFiles(corpusDir ?? string.Empty);
		IReadOnlyDictionary<string, byte>? quantizedFrequencies = null;

		if (corpusFiles.Count > 0)
		{
			if (cacheManager.Exists("frequencies"))
			{
				ui.ShowMessage("Loading frequencies from cache...");
				quantizedFrequencies = cacheManager.Load<Dictionary<string, byte>>("frequencies");
			}
			else
			{
				ui.ShowMessage("Analyzing frequencies (Parallel)... This may take a while.");
				var stopwatchFreq = Stopwatch.StartNew();
				var analyzer = new CorpusAnalyzer();
				var rawFreqs = analyzer.Analyze(corpusFiles, (current, total) => System.Console.Write($"\rProcessed {current}/{total} files..."));
				System.Console.WriteLine();

				var quantizer = new FrequencyQuantizer();
				quantizedFrequencies = quantizer.Quantize(rawFreqs);
				cacheManager.Save("frequencies", quantizedFrequencies);
				stopwatchFreq.Stop();
				ui.ShowSuccess($"Frequency analysis complete in {stopwatchFreq.Elapsed.TotalSeconds:F1}s.");
			}
		}

		var outputPath = fileLocator.GetOutputPath(selectedFile);
		ui.ShowMessage($"\nTarget output: {outputPath}");
		ui.ShowMessage("Compiling FST... This will use heavy CPU and RAM.");

		var stopwatchCompile = Stopwatch.StartNew();
		try
		{
			var lines = File.ReadLines(selectedFile);
			using var fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write);
			compiler.Compile(lines, fileStream, quantizedFrequencies);

			stopwatchCompile.Stop();
			ui.ShowSuccess($"Compilation finished in {stopwatchCompile.Elapsed.TotalSeconds:F1} s.");
			ui.ShowMessage($"Output file size: {new FileInfo(outputPath).Length / 1024.0 / 1024.0:F2} MB");
		}
		catch (Exception ex)
		{
			ui.ShowError($"Compilation failed: {ex.Message}");
		}

		ui.WaitForKey();
		ForceGarbageCollection();
	}

	private static void ForceGarbageCollection()
	{
		GC.Collect();
		GC.WaitForPendingFinalizers();
		GC.Collect();
	}
}
