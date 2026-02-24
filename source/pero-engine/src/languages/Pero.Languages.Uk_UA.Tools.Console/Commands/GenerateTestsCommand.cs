using System.Diagnostics;
using System.Text.Json;
using Pero.Languages.Uk_UA.Dictionaries;
using Pero.Languages.Uk_UA.Tools.Console.Models;
using Pero.Languages.Uk_UA.Tools.Console.Services;
using Pero.Languages.Uk_UA.Tools.Console.Services.Typo;
using Pero.Languages.Uk_UA.Tools.Console.UI;

namespace Pero.Languages.Uk_UA.Tools.Console.Commands;

public class GenerateTestsCommand
{
	private readonly ConsoleInterface _ui;
	private readonly FileLocator _fileLocator;
	private readonly Random _rnd = new();

	public GenerateTestsCommand(ConsoleInterface ui, FileLocator fileLocator)
	{
		_ui = ui;
		_fileLocator = fileLocator;
	}

	public void Execute()
	{
		_ui.ShowHeader("Generate Full-Sentence Spell-Check Tests");

		var dictFiles = _fileLocator.FindCompiledDictionaries();
		if (dictFiles.Count == 0)
		{
			_ui.ShowError("Compile the morphological dictionary first (.perodic).");
			_ui.WaitForKey();
			return;
		}

		var selectedDictPath = dictFiles[_ui.SelectOption("Select Dictionary", dictFiles.Select(Path.GetFileName).ToList()!)];
		var dictionary = new CompiledDictionary();
		using (var stream = new FileStream(selectedDictPath, FileMode.Open, FileAccess.Read))
		{
			dictionary.Load(stream);
		}

		var corpusDir = _ui.PromptInput("Enter path to corpus directory");
		var corpusFiles = _fileLocator.GetCorpusFiles(corpusDir ?? string.Empty);
		if (corpusFiles.Count == 0) return;

		int targetTests = _ui.PromptForInteger("How many test cases to generate?", 100, 50000);

		_ui.ShowMessage("\nHarvesting clean sentences and generating typos. This might take a minute...");

		var typoGenerator = new TypoGenerator(dictionary);
		var testSuite = new SpellingTestSuite();
		var uniqueInputs = new HashSet<string>();

		var sw = Stopwatch.StartNew();
		var shuffledFiles = corpusFiles.OrderBy(x => _rnd.Next()).ToList();

		foreach (var file in shuffledFiles)
		{
			if (testSuite.Cases.Count >= targetTests) break;

			using var reader = new StreamReader(new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read, 64 * 1024));
			string? line;
			while ((line = reader.ReadLine()) != null && testSuite.Cases.Count < targetTests)
			{
				var phrases = line.Split(new[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);

				foreach (var rawPhrase in phrases)
				{
					if (string.IsNullOrWhiteSpace(rawPhrase)) continue;
					var phrase = rawPhrase.Trim();

					var words = phrase.Split(new[] { ' ', ',', ';', ':', '(', ')', '"', '—', '-' }, StringSplitOptions.RemoveEmptyEntries)
									  .Select(w => w.Trim())
									  .Where(w => w.Length > 0)
									  .ToList();

					if (words.Count < 3 || words.Count > 15) continue;

					bool isClean = true;
					foreach (var word in words)
					{
						var lowerWord = word.ToLowerInvariant();
						if (lowerWord.Any(char.IsDigit) || !dictionary.Analyze(lowerWord).Any())
						{
							isClean = false;
							break;
						}
					}
					if (!isClean) continue;

					var validTargetIndices = new List<int>();
					for (int i = 0; i < words.Count; i++)
					{
						if (words[i].Length >= 4) validTargetIndices.Add(i);
					}

					if (validTargetIndices.Count == 0) continue;

					int targetIdx = validTargetIndices[_rnd.Next(validTargetIndices.Count)];
					string expectedWord = words[targetIdx].ToLowerInvariant();

					var typoResult = typoGenerator.Generate(expectedWord);
					if (typoResult == null) continue;

					// Reconstruct the original phrase with the typo injected
					// This preserves original punctuation and casing
					string typoWord = typoResult.Value.Typo;

					// Match case of the original word
					bool isFirstUpper = char.IsUpper(words[targetIdx][0]);
					bool isAllUpper = words[targetIdx].All(c => !char.IsLetter(c) || char.IsUpper(c));

					if (isAllUpper) typoWord = typoWord.ToUpperInvariant();
					else if (isFirstUpper) typoWord = char.ToUpperInvariant(typoWord[0]) + typoWord.Substring(1);

					// Use regex to replace the whole word to avoid partial matches
					string pattern = $@"\b{System.Text.RegularExpressions.Regex.Escape(words[targetIdx])}\b";
					string inputContext = System.Text.RegularExpressions.Regex.Replace(phrase, pattern, typoWord, System.Text.RegularExpressions.RegexOptions.None, TimeSpan.FromMilliseconds(100));

					if (uniqueInputs.Add(inputContext))
					{
						testSuite.Cases.Add(new SpellingTestCase
						{
							Name = $"{typoResult.Value.Type} | {expectedWord}",
							Input = inputContext,
							Expected = words[targetIdx] // Keep original case in expected
						});

						if (testSuite.Cases.Count % 100 == 0)
						{
							System.Console.Write($"\rGenerated {testSuite.Cases.Count} / {targetTests} ...");
						}

						if (testSuite.Cases.Count >= targetTests) break;
					}
				}
			}
		}

		sw.Stop();

		string outPath = Path.Combine(_fileLocator.BaseDirectory, "Compiled", "generated_spell_tests.json");
		var options = new JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
		string json = JsonSerializer.Serialize(testSuite, options);
		File.WriteAllText(outPath, json);

		_ui.ShowSuccess($"\n\nSuccessfully generated {testSuite.Cases.Count} tests in {sw.Elapsed.TotalSeconds:F1}s.");
		_ui.ShowMessage($"Saved to: {outPath}");
		_ui.WaitForKey();
	}
}