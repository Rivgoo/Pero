using System.Diagnostics;
using Pero.Languages.Uk_UA.Dictionaries;
using Pero.Languages.Uk_UA.Tools.Console.Services;
using Pero.Languages.Uk_UA.Tools.Console.UI;

namespace Pero.Languages.Uk_UA.Tools.Console.Commands;

public class BuildNgramsCommand
{
	private readonly ConsoleInterface _ui;
	private readonly FileLocator _fileLocator;
	private readonly NgramBuilder _builder;

	public BuildNgramsCommand(ConsoleInterface ui, FileLocator fileLocator)
	{
		_ui = ui;
		_fileLocator = fileLocator;
		_builder = new NgramBuilder();
	}

	public void Execute()
	{
		_ui.ShowHeader("Build N-gram Model (Filtered & Compressed)");

		// 1. Select Dictionary
		var dictFiles = _fileLocator.FindCompiledDictionaries();
		if (dictFiles.Count == 0)
		{
			_ui.ShowError("No compiled morphological dictionaries (.perodic) found!");
			_ui.ShowMessage("Please run 'Compile FST Dictionary' first.");
			_ui.WaitForKey();
			return;
		}

		_ui.ShowMessage("Select a dictionary to use for vocabulary filtering (removes junk words):");
		var selectedDictPath = dictFiles[_ui.SelectOption("Available Dictionaries", dictFiles.Select(Path.GetFileName).ToList()!)];

		// 2. Load Dictionary
		_ui.ShowMessage($"\nLoading dictionary: {Path.GetFileName(selectedDictPath)}...");
		var dictionary = new CompiledDictionary();
		try
		{
			using var stream = new FileStream(selectedDictPath, FileMode.Open, FileAccess.Read);
			dictionary.Load(stream);
			_ui.ShowSuccess("Dictionary loaded successfully.");
		}
		catch (Exception ex)
		{
			_ui.ShowError($"Failed to load dictionary: {ex.Message}");
			_ui.WaitForKey();
			return;
		}

		// 3. Select Corpus
		var corpusDir = _ui.PromptInput("Enter path to corpus directory");
		var corpusFiles = _fileLocator.GetCorpusFiles(corpusDir ?? string.Empty);

		if (corpusFiles.Count == 0)
		{
			_ui.ShowError("No text files found in the specified directory.");
			_ui.WaitForKey();
			return;
		}

		string outputPath = Path.Combine(_fileLocator.BaseDirectory, "Compiled", "uk_UA.perongram");
		Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
		string tempDir = Path.Combine(_fileLocator.BaseDirectory, ".cache");

		_ui.ShowMessage($"\nBuilding N-grams from {corpusFiles.Count} files...");

		var sw = Stopwatch.StartNew();
		try
		{
			_builder.Build(corpusFiles, dictionary, outputPath, tempDir, (current, total) =>
			{
				System.Console.Write($"\rProcessed {current}/{total} files...");
			});
			sw.Stop();

			_ui.ShowSuccess($"\nN-gram model built successfully in {sw.Elapsed.TotalMinutes:F2} minutes.");
			_ui.ShowMessage($"Output file size: {new FileInfo(outputPath).Length / 1024.0 / 1024.0:F2} MB");
		}
		catch (Exception ex)
		{
			_ui.ShowError($"\nBuild failed: {ex.Message}");
		}

		_ui.WaitForKey();
		GC.Collect();
	}
}