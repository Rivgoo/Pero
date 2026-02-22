using Pero.Languages.Uk_UA.Dictionaries;
using Pero.Languages.Uk_UA.Tools.Console.Services;
using Pero.Languages.Uk_UA.Tools.Console.UI;
using System.Diagnostics;

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

		var fileNames = dictFiles.Select(Path.GetFileName).ToList()!;
		var selectedIndex = _ui.SelectOption("Select a dictionary to load for measurement", fileNames);
		var selectedFile = dictFiles[selectedIndex];

		GC.Collect();
		GC.WaitForPendingFinalizers();
		GC.Collect();

		long memoryBefore = GC.GetTotalMemory(true);

		_ui.ShowMessage($"\nLoading dictionary: {selectedFile} ...");
		var dictionary = new CompiledDictionary();

		var stopwatch = Stopwatch.StartNew();
		using var fileStream = new FileStream(selectedFile, FileMode.Open, FileAccess.Read);
		dictionary.Load(fileStream);
		stopwatch.Stop();

		_ui.ShowMessage($"Dictionary loaded in {stopwatch.ElapsedMilliseconds} ms.");

		long memoryAfter = GC.GetTotalMemory(true);

		long memoryUsed = memoryAfter - memoryBefore;

		_ui.ShowSuccess($"\nApproximate memory footprint of the dictionary:");
		_ui.ShowMessage($"{memoryUsed:N0} bytes");
		_ui.ShowMessage($"{(memoryUsed / 1024.0):N2} KB");
		_ui.ShowMessage($"{(memoryUsed / 1024.0 / 1024.0):N2} MB");

		_ui.ShowMessage("\nNote: This is an estimation of the managed heap increase.");
		_ui.ShowMessage("For precise results, use a memory profiler.");

		_ui.WaitForKey();
		GC.KeepAlive(dictionary);
	}
}