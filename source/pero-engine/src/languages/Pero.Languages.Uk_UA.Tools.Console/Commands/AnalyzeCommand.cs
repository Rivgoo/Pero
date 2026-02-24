using Pero.Languages.Uk_UA.Dictionaries;
using Pero.Languages.Uk_UA.Tools.Console.Services;
using Pero.Languages.Uk_UA.Tools.Console.UI;
using System.Diagnostics;
using System.Text;

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

		var analysisMode = _ui.SelectOption("Select Report Mode", new List<string>
		{
			"Standard (Lookup only)",
			"Full (Lookup + Generate Paradigm)"
		});
		bool showParadigm = analysisMode == 1;

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
					_ui.ShowMessage($"\n>>> Result:");
					_ui.ShowMessage($"  Lemma:    {info.Lemma.ToUpperInvariant()}");
					_ui.ShowMessage($"  POS:      {info.Tagset.PartOfSpeech}");
					_ui.ShowMessage($"  Tags:     {FormatTags(info.Tagset)}");
				}

				if (showParadigm)
				{
					var uniqueLemmas = results.Select(r => r.Lemma).Distinct().ToList();

					foreach (var lemma in uniqueLemmas)
					{
						GenerateAndShowParadigm(dictionary, lemma);
					}
				}
			}
		}
	}

	private void GenerateAndShowParadigm(CompiledDictionary dictionary, string lemma)
	{
		System.Console.WriteLine();
		_ui.ShowMessage($"=== PARADIGM FOR: {lemma.ToUpperInvariant()} ===");

		var stopwatch = Stopwatch.StartNew();

		var allForms = dictionary.GetAllForms(lemma)
			.OrderBy(f => f.Tagset.Case)
			.ThenBy(f => f.Tagset.Number)
			.ToList();

		stopwatch.Stop();

		if (allForms.Count == 0)
		{
			_ui.ShowError("Paradigm data not found (Lemma FST missing or empty).");
			return;
		}

		string format = "{0,-20} | {1}";
		_ui.ShowMessage(string.Format(format, "WORD FORM", "MORPHOLOGICAL TAGS"));
		_ui.ShowMessage(new string('-', 60));

		foreach (var form in allForms)
		{
			_ui.ShowMessage(string.Format(format, form.Form, FormatTags(form.Tagset)));
		}

		_ui.ShowMessage(new string('-', 60));
		_ui.ShowMessage($"Total forms: {allForms.Count} | Generation time: {stopwatch.Elapsed.TotalMilliseconds:F4} ms");
	}

	private string FormatTags(Pero.Abstractions.Models.Morphology.MorphologyTagset t)
	{
		var sb = new StringBuilder();

		if (t.Case != Abstractions.Models.Morphology.GrammarCase.None) sb.Append($"Case:{t.Case} ");
		if (t.Gender != Abstractions.Models.Morphology.GrammarGender.None) sb.Append($"Gen:{t.Gender} ");
		if (t.Number != Abstractions.Models.Morphology.GrammarNumber.None) sb.Append($"Num:{t.Number} ");
		if (t.Person != Abstractions.Models.Morphology.GrammarPerson.None) sb.Append($"Pers:{t.Person} ");
		if (t.Tense != Abstractions.Models.Morphology.GrammarTense.None) sb.Append($"Tense:{t.Tense} ");

		if (t.Features != Abstractions.Models.Morphology.GrammarFeatures.None) sb.Append($"[{t.Features}]");

		return sb.ToString().Trim();
	}
}