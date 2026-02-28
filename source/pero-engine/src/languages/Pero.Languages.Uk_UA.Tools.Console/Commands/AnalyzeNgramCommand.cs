using System.Diagnostics;
using Pero.Kernel.Ngrams;
using Pero.Languages.Uk_UA.Tools.Console.Services;
using Pero.Languages.Uk_UA.Tools.Console.UI;
using Pero.Kernel.Utils;

namespace Pero.Languages.Uk_UA.Tools.Console.Commands;

public class AnalyzeNgramCommand
{
	private readonly ConsoleInterface _ui;
	private readonly FileLocator _fileLocator;

	public AnalyzeNgramCommand(ConsoleInterface ui, FileLocator fileLocator)
	{
		_ui = ui;
		_fileLocator = fileLocator;
	}

	public void Execute()
	{
		_ui.ShowHeader("Analyze N-gram Model");

		var files = Directory.GetFiles(Path.Combine(_fileLocator.BaseDirectory, "Compiled"), "*.perongram");
		if (files.Length == 0)
		{
			_ui.ShowError("No .perongram files found.");
			_ui.WaitForKey();
			return;
		}

		var selectedFile = files[_ui.SelectOption("Select a model to analyze", files.Select(Path.GetFileName).ToList()!)];

		_ui.ShowMessage($"\nLoading model: {Path.GetFileName(selectedFile)}...");
		var model = new NgramLanguageModel();
		var sw = Stopwatch.StartNew();

		try
		{
			using var stream = new FileStream(selectedFile, FileMode.Open, FileAccess.Read);
			model.Load(stream);
			sw.Stop();
		}
		catch (Exception ex)
		{
			_ui.ShowError($"Failed to load model: {ex.Message}");
			_ui.WaitForKey();
			return;
		}

		// Use reflection to peek private fields for stats (Analysis Tool Only!)
		var biType = model.GetType().GetField("_bigramHashes", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
		var triType = model.GetType().GetField("_trigramHashes", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

		var biCount = ((Array)biType!.GetValue(model)!).Length;
		var triCount = ((Array)triType!.GetValue(model)!).Length;

		_ui.ShowSuccess($"\nModel loaded in {sw.Elapsed.TotalMilliseconds:F2} ms.");
		_ui.ShowMessage("--- Statistics ---");
		_ui.ShowMessage($"Total Bigrams:  {biCount:N0}");
		_ui.ShowMessage($"Total Trigrams: {triCount:N0}");
		_ui.ShowMessage($"Total Records:  {biCount + triCount:N0}");
		_ui.ShowMessage($"File Size:      {new FileInfo(selectedFile).Length / 1024.0 / 1024.0:F2} MB");
		_ui.ShowMessage("------------------");

		while (true)
		{
			_ui.ShowMessage("\nTest N-gram (Enter 2 or 3 words separated by space, or 'q' to quit):");
			var input = _ui.PromptInput("> ");

			if (string.IsNullOrWhiteSpace(input)) continue;
			if (input.Trim().Equals("q", StringComparison.OrdinalIgnoreCase)) break;

			var words = input.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);

			if (words.Length == 2)
			{
				ulong h1 = MurmurHash3.Hash(words[0]);
				ulong h2 = MurmurHash3.Hash(words[1]);
				byte score = model.GetBigramScore(h1, h2);

				if (score > 0) _ui.ShowSuccess($"Bigram FOUND! Score: {score}/255");
				else _ui.ShowError("Bigram not found.");
			}
			else if (words.Length == 3)
			{
				ulong h1 = MurmurHash3.Hash(words[0]);
				ulong h2 = MurmurHash3.Hash(words[1]);
				ulong h3 = MurmurHash3.Hash(words[2]);
				byte score = model.GetTrigramScore(h1, h2, h3);

				if (score > 0) _ui.ShowSuccess($"Trigram FOUND! Score: {score}/255");
				else _ui.ShowError("Trigram not found.");
			}
			else
			{
				_ui.ShowMessage("Please enter exactly 2 or 3 words.");
			}
		}
	}
}