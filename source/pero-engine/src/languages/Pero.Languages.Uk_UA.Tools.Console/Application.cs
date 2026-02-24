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
	private readonly StressTestCommand _stressTestCommand;
	private readonly BuildNgramsCommand _buildNgramsCommand;
	private readonly AnalyzeNgramCommand _analyzeNgramCommand;
	private readonly GenerateTestsCommand _generateTestsCommand;

	public Application()
	{
		_ui = new ConsoleInterface();

		var currentDirectory = Directory.GetCurrentDirectory();
		var fileLocator = new FileLocator(currentDirectory);

		_compileCommand = new CompileCommand(_ui, fileLocator);
		_analyzeCommand = new AnalyzeCommand(_ui, fileLocator);
		_memoryUsageCommand = new MemoryUsageCommand(_ui, fileLocator);
		_stressTestCommand = new StressTestCommand(_ui, fileLocator);
		_buildNgramsCommand = new BuildNgramsCommand(_ui, fileLocator);
		_analyzeNgramCommand = new AnalyzeNgramCommand(_ui, fileLocator);
		_generateTestsCommand = new GenerateTestsCommand(_ui, fileLocator);
	}

	public void Run()
	{
		var options = new List<string>
		{
			"Compile raw text dictionary to binary format",
			"Build N-gram model from corpus (.perongram)",
			"Load binary dictionary and analyze words",
			"Measure dictionary memory usage",
			"Run dictionary performance stress test",
			"Analyze N-gram model stats & lookup",
			"Generate Automated Spell-Check Tests (JSON)",
			"Exit"
		};

		while (true)
		{
			_ui.ShowHeader("Pero Dictionary Tools");

			var selection = _ui.SelectOption("Choose an action", options);

			switch (selection)
			{
				case 0: _compileCommand.Execute(); break;
				case 1: _buildNgramsCommand.Execute(); break;
				case 2: _analyzeCommand.Execute(); break;
				case 3: _memoryUsageCommand.Execute(); break;
				case 4: _stressTestCommand.Execute(); break;
				case 5: _analyzeNgramCommand.Execute(); break;
				case 6: _generateTestsCommand.Execute(); break;
				case 7: _ui.ShowMessage("Exiting..."); return;
			}
		}
	}
}