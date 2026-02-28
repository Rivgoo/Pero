using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using Pero.Abstractions.Models.Morphology;
using Pero.Kernel.Dictionaries;

namespace Pero.Tools.Compiler.Services;

public class NgramBuilderFacade<TTag> where TTag : MorphologicalTag
{
	private const int MinBigramFrequency = 10;
	private const int MinTrigramFrequency = 7;
	private const long MaxMemoryUsageBytes = 7_500_000_000;

	private readonly NgramCounter<TTag> counter;
	private readonly NgramCompressor compressor;

	public NgramBuilderFacade(NgramCounter<TTag> counter, NgramCompressor compressor)
	{
		this.counter = counter;
		this.compressor = compressor;
	}

	public void Build(IReadOnlyList<string> corpusFiles, FstSuffixDictionary<TTag> dictionary, string outputPath, string tempDir, Action<int, int> progressCallback)
	{
		Directory.CreateDirectory(tempDir);
		var bigramChunkPaths = new ConcurrentBag<string>();
		var trigramChunkPaths = new ConcurrentBag<string>();

		int processedCount = 0;
		object memoryCheckLock = new();

		var globalBigrams = new Dictionary<ulong, int>(10_000_000);
		var globalTrigrams = new Dictionary<ulong, int>(10_000_000);
		object mergeLock = new();

		Parallel.ForEach(corpusFiles, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, file =>
		{
			var localBigrams = new Dictionary<ulong, int>();
			var localTrigrams = new Dictionary<ulong, int>();

			counter.ProcessFile(file, dictionary, localBigrams, localTrigrams);

			bool memoryLimitReached = false;
			lock (mergeLock)
			{
				foreach (var kvp in localBigrams)
				{
					ref int val = ref CollectionsMarshal.GetValueRefOrAddDefault(globalBigrams, kvp.Key, out _);
					val += kvp.Value;
				}
				foreach (var kvp in localTrigrams)
				{
					ref int val = ref CollectionsMarshal.GetValueRefOrAddDefault(globalTrigrams, kvp.Key, out _);
					val += kvp.Value;
				}

				if (GC.GetTotalMemory(false) > MaxMemoryUsageBytes) memoryLimitReached = true;
			}

			if (memoryLimitReached)
			{
				lock (memoryCheckLock)
				{
					if (GC.GetTotalMemory(false) > MaxMemoryUsageBytes)
					{
						Dictionary<ulong, int> biDump, triDump;
						lock (mergeLock)
						{
							biDump = globalBigrams;
							triDump = globalTrigrams;
							globalBigrams = new Dictionary<ulong, int>(10_000_000);
							globalTrigrams = new Dictionary<ulong, int>(10_000_000);
						}

						Console.WriteLine($"\n[RAM Limit] Dumping chunk...");
						bigramChunkPaths.Add(DumpChunk(biDump, tempDir, "bi"));
						trigramChunkPaths.Add(DumpChunk(triDump, tempDir, "tri"));

						biDump = null; triDump = null;
						GC.Collect(2, GCCollectionMode.Forced, true);
					}
				}
			}

			int current = Interlocked.Increment(ref processedCount);
			progressCallback(current, corpusFiles.Count);
		});

		if (globalBigrams.Count > 0)
		{
			Console.WriteLine($"\nDumping final chunk...");
			bigramChunkPaths.Add(DumpChunk(globalBigrams, tempDir, "bi"));
			trigramChunkPaths.Add(DumpChunk(globalTrigrams, tempDir, "tri"));
		}

		globalBigrams = null; globalTrigrams = null;
		GC.Collect();

		Console.WriteLine("\nMerging chunks and reducing...");

		string finalBigramPath = Path.Combine(tempDir, "merged_bigrams.bin");
		string finalTrigramPath = Path.Combine(tempDir, "merged_trigrams.bin");

		int bigramCount = compressor.MergeChunks(bigramChunkPaths.ToList(), finalBigramPath, MinBigramFrequency);
		int trigramCount = compressor.MergeChunks(trigramChunkPaths.ToList(), finalTrigramPath, MinTrigramFrequency);

		compressor.WriteFinalFile(outputPath, finalBigramPath, bigramCount, finalTrigramPath, trigramCount, tempDir);

		foreach (var f in bigramChunkPaths) File.Delete(f);
		foreach (var f in trigramChunkPaths) File.Delete(f);
		File.Delete(finalBigramPath); File.Delete(finalTrigramPath);
	}

	private string DumpChunk(Dictionary<ulong, int> data, string tempDir, string prefix)
	{
		string path = Path.Combine(tempDir, $"{prefix}_{Guid.NewGuid()}.tmp");
		var sortedKeys = data.Keys.ToArray();
		Array.Sort(sortedKeys);

		using var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 64 * 1024);
		using var writer = new BinaryWriter(fs);

		foreach (var key in sortedKeys)
		{
			writer.Write(key);
			writer.Write(data[key]);
		}
		return path;
	}
}