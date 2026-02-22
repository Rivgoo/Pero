using Pero.Languages.Uk_UA.Tools.Console.Commands;
using Pero.Languages.Uk_UA.Tools.Console.Services;
using Pero.Languages.Uk_UA.Tools.Console.UI;

namespace Pero.Languages.Uk_UA.Tools.Console;

public class Application
{
	private readonly ConsoleInterface _ui;
	private readonly CompileCommand _compileCommand;
	private readonly AnalyzeCommand _analyzeCommand;
	private readonly MemoryUsageCommand _memoryUsageCommand;

	public Application()
	{
		_ui = new ConsoleInterface();

		var currentDirectory = Directory.GetCurrentDirectory();
		var fileLocator = new FileLocator(currentDirectory);

		_compileCommand = new CompileCommand(_ui, fileLocator);
		_analyzeCommand = new AnalyzeCommand(_ui, fileLocator);
		_memoryUsageCommand = new MemoryUsageCommand(_ui, fileLocator);
	}

	public void Run()
	{
		var options = new List<string>
		{
			"Compile raw text dictionary to binary format",
			"Load binary dictionary and analyze words",
			"Measure dictionary memory usage",
			"Exit"
		};

		while (true)
		{
			_ui.ShowHeader("Pero Dictionary Tools");

			var selection = _ui.SelectOption("Choose an action", options);

			switch (selection)
			{
				case 0:
					_compileCommand.Execute();
					break;
				case 1:
					_analyzeCommand.Execute();
					break;
				case 2:
					_memoryUsageCommand.Execute();
					break;
				case 3:
					_ui.ShowMessage("Exiting...");
					return;
			}
		}
	}
}