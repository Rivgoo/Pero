using System.Collections.Concurrent;

namespace Pero.Languages.Uk_UA.Tools.Console.Services;

public class CorpusAnalyzer
{
	public IReadOnlyDictionary<string, long> Analyze(IReadOnlyList<string> filePaths, Action<int, int> progressCallback)
	{
		var globalCounts = new ConcurrentDictionary<string, long>(StringComparer.OrdinalIgnoreCase);
		int processedFiles = 0;

		var parallelOptions = new ParallelOptions
		{
			MaxDegreeOfParallelism = Environment.ProcessorCount
		};

		Parallel.ForEach(filePaths, parallelOptions, file =>
		{
			var localCounts = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);

			using var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read, 1024 * 1024); // 1MB buffer
			using var reader = new StreamReader(stream);

			string? line;
			while ((line = reader.ReadLine()) != null)
			{
				ParseLineAndCount(line, localCounts);
			}

			foreach (var kvp in localCounts)
			{
				globalCounts.AddOrUpdate(
					kvp.Key,
					kvp.Value,
					(_, existingValue) => existingValue + kvp.Value);
			}

			int current = Interlocked.Increment(ref processedFiles);
			progressCallback(current, filePaths.Count);
		});

		return globalCounts;
	}

	private static void ParseLineAndCount(ReadOnlySpan<char> line, Dictionary<string, long> localCounts)
	{
		Span<char> wordBuffer = stackalloc char[256];
		int length = 0;

		for (int i = 0; i <= line.Length; i++)
		{
			bool isWordChar = false;
			char c = '\0';

			if (i < line.Length)
			{
				c = line[i];
				isWordChar = char.IsLetter(c) || IsApostrophe(c);
			}

			if (isWordChar)
			{
				if (length < 256)
				{
					wordBuffer[length++] = char.ToLowerInvariant(c);
				}
			}
			else if (length > 0)
			{
				int startIdx = 0;
				int endIdx = length - 1;

				while (startIdx <= endIdx && IsApostrophe(wordBuffer[startIdx])) startIdx++;
				while (endIdx >= startIdx && IsApostrophe(wordBuffer[endIdx])) endIdx--;

				int finalLen = endIdx - startIdx + 1;
				if (finalLen > 0)
				{
					var wordSpan = wordBuffer.Slice(startIdx, finalLen);
					var word = new string(wordSpan);

					if (localCounts.TryGetValue(word, out long count))
						localCounts[word] = count + 1;
					else
						localCounts[word] = 1;
				}

				length = 0;
			}
		}
	}

	private static bool IsApostrophe(char c)
	{
		return c == '\'' || c == '’' || c == 'ʼ';
	}
}