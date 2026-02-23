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
	private readonly CacheManager _cacheManager;

	public CompileCommand(ConsoleInterface ui, FileLocator fileLocator)
	{
		_ui = ui;
		_fileLocator = fileLocator;
		_compiler = new DictionaryCompiler();
		_cacheManager = new CacheManager(fileLocator.BaseDirectory);
	}

	public void Execute()
	{
		_ui.ShowHeader("Compile FST Dictionary (with Caching)");

		var textFiles = _fileLocator.FindTextFiles();
		if (textFiles.Count == 0)
		{
			_ui.ShowError($"No .txt files found in {_fileLocator.BaseDirectory}");
			_ui.WaitForKey();
			return;
		}
		var selectedFile = textFiles[_ui.SelectOption("Select a source file to compile", textFiles.Select(Path.GetFileName).ToList()!)];

		var corpusDir = _ui.PromptInput("Enter path to corpus directory for frequency analysis (leave empty to skip)");
		var corpusFiles = _fileLocator.GetCorpusFiles(corpusDir ?? string.Empty);
		IReadOnlyDictionary<string, byte>? quantizedFrequencies = null;

		if (corpusFiles.Count > 0)
		{
			if (_cacheManager.Exists("frequencies"))
			{
				_ui.ShowMessage("Loading frequencies from cache...");
				quantizedFrequencies = _cacheManager.Load<Dictionary<string, byte>>("frequencies");
			}
			else
			{
				_ui.ShowMessage("Analyzing frequencies (Parallel)... This may take a while.");
				var stopwatchFreq = Stopwatch.StartNew();
				var analyzer = new CorpusAnalyzer();
				var rawFreqs = analyzer.Analyze(corpusFiles, (current, total) => System.Console.Write($"\rProcessed {current}/{total} files..."));
				System.Console.WriteLine();

				var quantizer = new FrequencyQuantizer();
				quantizedFrequencies = quantizer.Quantize(rawFreqs);
				_cacheManager.Save("frequencies", quantizedFrequencies);
				stopwatchFreq.Stop();
				_ui.ShowSuccess($"Frequency analysis complete in {stopwatchFreq.Elapsed.TotalSeconds:F1}s.");
			}
		}

		var outputPath = _fileLocator.GetOutputPath(selectedFile);
		_ui.ShowMessage($"\nTarget output: {outputPath}");
		_ui.ShowMessage("Compiling FST... This will use heavy CPU and RAM.");

		var stopwatchCompile = Stopwatch.StartNew();
		try
		{
			var lines = File.ReadLines(selectedFile);
			using var fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write);
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