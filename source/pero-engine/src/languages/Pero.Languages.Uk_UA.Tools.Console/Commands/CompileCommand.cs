using System.Diagnostics;
using Pero.Languages.Uk_UA.Dictionaries.Builder;
using Pero.Languages.Uk_UA.Tools.Console.Services;
using Pero.Languages.Uk_UA.Tools.Console.UI;

namespace Pero.Languages.Uk_UA.Tools.Console.Commands;

public class CompileCommand
{
	private readonly ConsoleInterface _ui;
	private readonly FileLocator _fileLocator;
	private readonly DictionaryCompiler _compiler;

	public CompileCommand(ConsoleInterface ui, FileLocator fileLocator)
	{
		_ui = ui;
		_fileLocator = fileLocator;
		_compiler = new DictionaryCompiler();
	}

	public void Execute()
	{
		_ui.ShowHeader("Compile Dictionary");

		var textFiles = _fileLocator.FindTextFiles();
		if (textFiles.Count == 0)
		{
			_ui.ShowMessage($"No .txt files found in {_fileLocator.BaseDirectory}");
			_ui.WaitForKey();
			return;
		}

		var fileNames = textFiles.Select(Path.GetFileName).ToList()!;
		var selectedIndex = _ui.SelectOption("Select a source file to compile", fileNames);
		var selectedFile = textFiles[selectedIndex];

		IReadOnlyDictionary<string, byte>? quantizedFrequencies = null;

		var corpusDir = _ui.PromptInput("Enter path to corpus directory for frequency analysis (leave empty to skip)");
		var corpusFiles = _fileLocator.GetCorpusFiles(corpusDir ?? string.Empty);

		if (corpusFiles.Count > 0)
		{
			_ui.ShowMessage($"\nFound {corpusFiles.Count} text files in corpus. Analyzing frequencies (Parallel)...");
			var stopwatchFreq = Stopwatch.StartNew();

			var analyzer = new CorpusAnalyzer();
			var rawFreqs = analyzer.Analyze(corpusFiles, (current, total) =>
			{
				System.Console.Write($"\rProcessed {current}/{total} files...");
			});

			System.Console.WriteLine();
			_ui.ShowMessage("Quantizing frequencies...");

			var quantizer = new FrequencyQuantizer();
			quantizedFrequencies = quantizer.Quantize(rawFreqs);

			stopwatchFreq.Stop();
			_ui.ShowSuccess($"Frequency analysis complete in {stopwatchFreq.Elapsed.TotalSeconds:F1} s. Found {rawFreqs.Count} unique words.");

			// Free massive memory structures before FST building
			rawFreqs = null;
			ForceGarbageCollection();
		}
		else if (!string.IsNullOrWhiteSpace(corpusDir))
		{
			_ui.ShowError("Directory not found or empty. Proceeding without frequency analysis.");
		}

		var outputPath = _fileLocator.GetOutputPath(selectedFile);

		_ui.ShowMessage($"\nReading source: {selectedFile}");
		_ui.ShowMessage($"Target output: {outputPath}");
		_ui.ShowMessage("\nCompiling (Phase 1: Parsing)...");

		var stopwatchCompile = Stopwatch.StartNew();

		try
		{
			var lines = File.ReadLines(selectedFile);

			using var fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write);

			// Added UI callbacks inside compiler if needed, but for now we rely on explicit messages
			_ui.ShowMessage("Compiling (Phase 2: Sorting and Building FST Graph)... This will take heavy CPU usage.");

			_compiler.Compile(lines, fileStream, quantizedFrequencies);

			stopwatchCompile.Stop();
			_ui.ShowSuccess($"Compilation finished in {stopwatchCompile.Elapsed.TotalSeconds:F1} s.");
			_ui.ShowMessage($"Output file size: {new FileInfo(outputPath).Length / 1024.0 / 1024.0:F2} MB");
		}
		catch (Exception ex)
		{
			_ui.ShowError($"Compilation failed: {ex.Message}");
		}

		_ui.WaitForKey();
		ForceGarbageCollection();
	}

	private static void ForceGarbageCollection()
	{
		GC.Collect();
		GC.WaitForPendingFinalizers();
		GC.Collect();
	}
}