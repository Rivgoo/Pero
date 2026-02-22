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

		var outputPath = _fileLocator.GetOutputPath(selectedFile);

		_ui.ShowMessage($"\nReading source: {selectedFile}");
		_ui.ShowMessage($"Target output: {outputPath}");
		_ui.ShowMessage("\nCompiling... This may take a minute.");

		var stopwatch = Stopwatch.StartNew();

		try
		{
			var lines = File.ReadLines(selectedFile);

			using var fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write);
			_compiler.Compile(lines, fileStream);

			stopwatch.Stop();
			_ui.ShowSuccess($"Compilation finished in {stopwatch.ElapsedMilliseconds} ms.");
			_ui.ShowMessage($"Output file size: {new FileInfo(outputPath).Length / 1024.0 / 1024.0:F2} MB");
		}
		catch (Exception ex)
		{
			_ui.ShowError($"Compilation failed: {ex.Message}");
		}

		_ui.WaitForKey();
	}
}