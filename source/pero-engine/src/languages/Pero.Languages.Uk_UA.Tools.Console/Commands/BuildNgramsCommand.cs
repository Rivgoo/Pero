using Pero.Kernel.Dictionaries;
using Pero.Languages.Uk_UA.Models.Morphology;
using Pero.Languages.Uk_UA.Tools.Console.Services;
using Pero.Languages.Uk_UA.Tools.Console.UI;
using Pero.Tools.Compiler.Services;
using System.Diagnostics;

namespace Pero.Languages.Uk_UA.Tools.Console.Commands;

public class BuildNgramsCommand
{
	private readonly ConsoleInterface ui;
	private readonly FileLocator fileLocator;
	private readonly NgramBuilderFacade<UkMorphologyTag> builder;

	public BuildNgramsCommand(ConsoleInterface ui, FileLocator fileLocator, NgramBuilderFacade<UkMorphologyTag> builder)
	{
		this.ui = ui;
		this.fileLocator = fileLocator;
		this.builder = builder;
	}

	public void Execute()
	{
		ui.ShowHeader("Build N-gram Model (Filtered & Compressed)");

		var dictFiles = fileLocator.FindCompiledDictionaries();
		if (dictFiles.Count == 0)
		{
			ui.ShowError("No compiled morphological dictionaries (.perodic) found!");
			ui.ShowMessage("Please run 'Compile FST Dictionary' first.");
			ui.WaitForKey();
			return;
		}

		ui.ShowMessage("Select a dictionary to use for vocabulary filtering (removes junk words):");
		var selectedDictPath = dictFiles[ui.SelectOption("Available Dictionaries", dictFiles.Select(Path.GetFileName).ToList()!)];

		ui.ShowMessage($"\nLoading dictionary: {Path.GetFileName(selectedDictPath)}...");
		var dictionary = new FstSuffixDictionary<UkMorphologyTag>();
		var decoder = new UkMorphologyDecoder();
		try
		{
			using var stream = new FileStream(selectedDictPath, FileMode.Open, FileAccess.Read);
			dictionary.Load(stream, decoder);
			ui.ShowSuccess("Dictionary loaded successfully.");
		}
		catch (Exception ex)
		{
			ui.ShowError($"Failed to load dictionary: {ex.Message}");
			ui.WaitForKey();
			return;
		}

		var corpusDir = ui.PromptInput("Enter path to corpus directory");
		var corpusFiles = fileLocator.GetCorpusFiles(corpusDir ?? string.Empty);

		if (corpusFiles.Count == 0)
		{
			ui.ShowError("No text files found in the specified directory.");
			ui.WaitForKey();
			return;
		}

		string outputPath = Path.Combine(fileLocator.BaseDirectory, "Compiled", "output.perongram");
		Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
		string tempDir = Path.Combine(fileLocator.BaseDirectory, ".cache");

		ui.ShowMessage($"\nBuilding N-grams from {corpusFiles.Count} files...");

		var sw = Stopwatch.StartNew();
		try
		{
			builder.Build(corpusFiles, dictionary, outputPath, tempDir, (current, total) =>
			{
				System.Console.Write($"\rProcessed {current}/{total} files...");
			});
			sw.Stop();

			ui.ShowSuccess($"\nN-gram model built successfully in {sw.Elapsed.TotalMinutes:F2} minutes.");
			ui.ShowMessage($"Output file size: {new FileInfo(outputPath).Length / 1024.0 / 1024.0:F2} MB");
		}
		catch (Exception ex)
		{
			ui.ShowError($"\nBuild failed: {ex.Message}");
		}

		ui.WaitForKey();
		GC.Collect();
	}
}