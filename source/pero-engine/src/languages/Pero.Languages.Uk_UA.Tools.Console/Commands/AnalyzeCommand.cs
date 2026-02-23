using Pero.Languages.Uk_UA.Dictionaries;
using Pero.Languages.Uk_UA.Tools.Console.Services;
using Pero.Languages.Uk_UA.Tools.Console.UI;
using System.Diagnostics;

namespace Pero.Languages.Uk_UA.Tools.Console.Commands;

public class AnalyzeCommand
{
	private readonly ConsoleInterface _ui;
	private readonly FileLocator _fileLocator;

	public AnalyzeCommand(ConsoleInterface ui, FileLocator fileLocator)
	{
		_ui = ui;
		_fileLocator = fileLocator;
	}

	public void Execute()
	{
		_ui.ShowHeader("Analyze Word");

		var dictFiles = _fileLocator.FindCompiledDictionaries();

		if (dictFiles.Count == 0)
		{
			_ui.ShowMessage("No compiled dictionaries found. Please compile a text file first.");
			_ui.WaitForKey();
			return;
		}

		var selectedFile = dictFiles[_ui.SelectOption("Select a dictionary", dictFiles.Select(Path.GetFileName).ToList()!)];

		_ui.ShowMessage($"\nLoading dictionary: {selectedFile} ...");

		var dictionary = new CompiledDictionary();
		var stopwatch = Stopwatch.StartNew();

		try
		{
			using var fileStream = new FileStream(selectedFile, FileMode.Open, FileAccess.Read);
			dictionary.Load(fileStream);

			stopwatch.Stop();
			_ui.ShowSuccess($"Dictionary loaded in {stopwatch.ElapsedMilliseconds} ms.");
		}
		catch (Exception ex)
		{
			_ui.ShowError($"Failed to load: {ex.Message}");
			_ui.WaitForKey();
			return;
		}

		while (true)
		{
			_ui.ShowMessage("\n" + new string('-', 30));
			var word = _ui.PromptInput("Enter a word to analyze (or 'q' to quit)");

			if (string.IsNullOrWhiteSpace(word)) continue;
			if (word.Trim().Equals("q", StringComparison.OrdinalIgnoreCase)) break;

			word = word.Trim().ToLowerInvariant();

			stopwatch.Restart();
			var results = dictionary.Analyze(word).ToList();
			stopwatch.Stop();

			if (results.Count == 0)
			{
				_ui.ShowError($"Word '{word}' not found in {stopwatch.Elapsed.TotalMilliseconds:F4} ms.");
			}
			else
			{
				_ui.ShowSuccess($"Found {results.Count} variant(s) in {stopwatch.Elapsed.TotalMilliseconds:F4} ms:");
				foreach (var info in results)
				{
					_ui.ShowMessage($"- Lemma: {info.Lemma}");
					_ui.ShowMessage($"  POS: {info.Tagset.PartOfSpeech}");
					_ui.ShowMessage($"  Case: {info.Tagset.Case}");
					_ui.ShowMessage($"  Gender: {info.Tagset.Gender}");
					_ui.ShowMessage($"  Features: {info.Tagset.Features}");
				}
			}
		}
	}
}