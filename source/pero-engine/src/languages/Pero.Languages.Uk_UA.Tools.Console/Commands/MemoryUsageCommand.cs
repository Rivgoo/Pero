using Pero.Languages.Uk_UA.Dictionaries;
using Pero.Languages.Uk_UA.Tools.Console.Services;
using Pero.Languages.Uk_UA.Tools.Console.UI;

namespace Pero.Languages.Uk_UA.Tools.Console.Commands;

public class MemoryUsageCommand
{
	private readonly ConsoleInterface _ui;
	private readonly FileLocator _fileLocator;

	public MemoryUsageCommand(ConsoleInterface ui, FileLocator fileLocator)
	{
		_ui = ui;
		_fileLocator = fileLocator;
	}

	public void Execute()
	{
		_ui.ShowHeader("Measure Dictionary Memory Usage");

		var dictFiles = _fileLocator.FindCompiledDictionaries();
		if (dictFiles.Count == 0)
		{
			_ui.ShowMessage("No compiled dictionaries found. Please compile a text file first.");
			_ui.WaitForKey();
			return;
		}

		var selectedFile = dictFiles[_ui.SelectOption("Select dictionary", dictFiles.Select(Path.GetFileName).ToList()!)];

		GC.Collect();
		GC.WaitForPendingFinalizers();
		GC.Collect();

		long memoryBefore = GC.GetTotalMemory(true);

		var dictionary = new CompiledDictionary();
		using var fileStream = new FileStream(selectedFile, FileMode.Open, FileAccess.Read);
		dictionary.Load(fileStream);

		long memoryAfter = GC.GetTotalMemory(true);
		long memoryUsed = memoryAfter - memoryBefore;

		_ui.ShowSuccess($"\nApproximate heap footprint of loaded arrays:");
		_ui.ShowMessage($"{memoryUsed:N0} bytes");
		_ui.ShowMessage($"{(memoryUsed / 1024.0 / 1024.0):N2} MB");

		_ui.WaitForKey();
		GC.KeepAlive(dictionary);
	}
}