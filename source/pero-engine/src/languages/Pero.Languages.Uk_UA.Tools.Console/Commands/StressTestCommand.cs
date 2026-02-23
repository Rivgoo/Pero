using System.Diagnostics;
using Pero.Languages.Uk_UA.Dictionaries;
using Pero.Languages.Uk_UA.Tools.Console.Services;
using Pero.Languages.Uk_UA.Tools.Console.UI;

namespace Pero.Languages.Uk_UA.Tools.Console.Commands;

public class StressTestCommand
{
	private readonly ConsoleInterface _ui;
	private readonly FileLocator _fileLocator;
	private readonly Random _random = new();

	public StressTestCommand(ConsoleInterface ui, FileLocator fileLocator)
	{
		_ui = ui;
		_fileLocator = fileLocator;
	}

	public void Execute()
	{
		_ui.ShowHeader("Stress Test Dictionary Performance");

		// 1. Select source text dictionary to get test words
		var textFiles = _fileLocator.FindTextFiles();
		if (textFiles.Count == 0)
		{
			_ui.ShowError("No source .txt dictionaries found to prepare test data.");
			_ui.WaitForKey();
			return;
		}
		var textFile = textFiles[_ui.SelectOption("Select a source .txt to get words from", textFiles.Select(Path.GetFileName).ToList()!)];

		// 2. Select compiled binary dictionary to test
		var dictFiles = _fileLocator.FindCompiledDictionaries();
		if (dictFiles.Count == 0)
		{
			_ui.ShowError("No compiled .perodic dictionaries found to test against.");
			_ui.WaitForKey();
			return;
		}
		var dictFile = dictFiles[_ui.SelectOption("Select a compiled .perodic dictionary to test", dictFiles.Select(Path.GetFileName).ToList()!)];

		// 3. Prepare test words
		_ui.ShowMessage($"\nReading unique words from '{Path.GetFileName(textFile)}'...");
		var allUniqueWords = ReadAllUniqueWords(textFile);
		_ui.ShowMessage($"Found {allUniqueWords.Count:N0} unique words available for testing.");

		int wordsToTest = _ui.PromptForInteger("Enter the number of words to test", 1, allUniqueWords.Count);

		Shuffle(allUniqueWords);
		var testWords = allUniqueWords.Take(wordsToTest).ToList();
		_ui.ShowMessage($"Selected {testWords.Count:N0} random words for the test.");

		// 4. Run the test
		_ui.ShowMessage("Loading binary dictionary into memory...");
		var dictionary = new CompiledDictionary();
		using (var fileStream = new FileStream(dictFile, FileMode.Open, FileAccess.Read))
		{
			dictionary.Load(fileStream);
		}

		_ui.ShowMessage("Starting performance measurement...");

		var stopwatch = Stopwatch.StartNew();
		foreach (var word in testWords)
		{
			dictionary.Analyze(word);
		}
		stopwatch.Stop();

		// 5. Report results
		var totalMs = stopwatch.Elapsed.TotalMilliseconds;
		var avgMs = totalMs / wordsToTest;
		var avgNs = (totalMs * 1_000_000) / wordsToTest;
		var opsPerSecond = wordsToTest / stopwatch.Elapsed.TotalSeconds;

		_ui.ShowSuccess("\n--- Stress Test Complete ---");
		_ui.ShowMessage($"Total Lookups:    {wordsToTest:N0}");
		_ui.ShowMessage($"Total Time:       {totalMs:F4} ms");
		_ui.ShowMessage($"Avg Time/Word:    {avgNs:F2} ns");
		_ui.ShowMessage($"Lookups/Second:   {opsPerSecond:N0} ops/sec");
		_ui.ShowMessage("--------------------------");

		_ui.WaitForKey();
	}

	private List<string> ReadAllUniqueWords(string filePath)
	{
		return File.ReadLines(filePath)
			.Where(line => !string.IsNullOrWhiteSpace(line))
			.Select(line => line.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0])
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.ToList();
	}

	private void Shuffle(List<string> list)
	{
		int n = list.Count;
		while (n > 1)
		{
			n--;
			int k = _random.Next(n + 1);
			(list[k], list[n]) = (list[n], list[k]);
		}
	}
}