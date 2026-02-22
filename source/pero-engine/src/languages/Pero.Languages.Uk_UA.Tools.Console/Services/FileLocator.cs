using Pero.Languages.Uk_UA.Tools.Console.Constants;

namespace Pero.Languages.Uk_UA.Tools.Console.Services;

public class FileLocator
{
	public string BaseDirectory { get; }

	public FileLocator(string baseDirectory)
	{
		BaseDirectory = baseDirectory;
	}

	public IReadOnlyList<string> FindTextFiles()
	{
		return Directory.GetFiles(BaseDirectory, AppConstants.TextFileExtension);
	}

	public IReadOnlyList<string> FindCompiledDictionaries()
	{
		var compiledDir = GetCompiledDirectoryPath();
		if (!Directory.Exists(compiledDir)) return Array.Empty<string>();

		return Directory.GetFiles(compiledDir, AppConstants.DictionaryFileExtension);
	}

	public IReadOnlyList<string> GetCorpusFiles(string directoryPath)
	{
		if (string.IsNullOrWhiteSpace(directoryPath) || !Directory.Exists(directoryPath))
		{
			return Array.Empty<string>();
		}

		return Directory.GetFiles(directoryPath, AppConstants.TextFileExtension, SearchOption.AllDirectories);
	}

	public string GetOutputPath(string sourceFileName)
	{
		var compiledDir = GetCompiledDirectoryPath();
		Directory.CreateDirectory(compiledDir);

		var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(sourceFileName);
		var newFileName = $"{fileNameWithoutExtension}{AppConstants.DictionaryFileExtension.Trim('*')}";

		return Path.Combine(compiledDir, newFileName);
	}

	private string GetCompiledDirectoryPath()
	{
		return Path.Combine(BaseDirectory, AppConstants.CompiledDirectoryName);
	}
}