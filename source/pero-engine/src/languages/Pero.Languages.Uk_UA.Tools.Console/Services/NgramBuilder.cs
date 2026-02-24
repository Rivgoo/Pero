using System.Collections.Concurrent;
using System.IO.Compression;
using System.Runtime.InteropServices;
using Pero.Languages.Uk_UA.Dictionaries;
using Pero.Languages.Uk_UA.Utils;

namespace Pero.Languages.Uk_UA.Tools.Console.Services;

public class NgramBuilder
{
	private const int MinBigramFrequency = 10;
	private const int MinTrigramFrequency = 7;
	private const long MaxMemoryUsageBytes = 7_500_000_000;

	public void Build(IReadOnlyList<string> corpusFiles, CompiledDictionary dictionary, string outputPath, string tempDir, Action<int, int> progressCallback)
	{
		Directory.CreateDirectory(tempDir);
		var bigramChunkPaths = new ConcurrentBag<string>();
		var trigramChunkPaths = new ConcurrentBag<string>();

		int processedCount = 0;
		object memoryCheckLock = new();

		var globalBigrams = new Dictionary<ulong, int>(10_000_000);
		var globalTrigrams = new Dictionary<ulong, int>(10_000_000);
		object mergeLock = new();

		var wordValidationCache = new ConcurrentDictionary<string, bool>(StringComparer.Ordinal);

		Parallel.ForEach(corpusFiles, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, file =>
		{
			var localBigrams = new Dictionary<ulong, int>();
			var localTrigrams = new Dictionary<ulong, int>();

			ParseFile(file, dictionary, wordValidationCache, localBigrams, localTrigrams);

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

						System.Console.WriteLine($"\n[RAM Limit] Dumping chunk...");
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
			System.Console.WriteLine($"\nDumping final chunk...");
			bigramChunkPaths.Add(DumpChunk(globalBigrams, tempDir, "bi"));
			trigramChunkPaths.Add(DumpChunk(globalTrigrams, tempDir, "tri"));
		}

		globalBigrams = null; globalTrigrams = null; wordValidationCache = null;
		GC.Collect();

		System.Console.WriteLine("\nMerging chunks and reducing (Pruning threshold applied)...");

		string finalBigramPath = Path.Combine(tempDir, "merged_bigrams.bin");
		string finalTrigramPath = Path.Combine(tempDir, "merged_trigrams.bin");

		int bigramCount = MergeChunks(bigramChunkPaths.ToList(), finalBigramPath, MinBigramFrequency);
		int trigramCount = MergeChunks(trigramChunkPaths.ToList(), finalTrigramPath, MinTrigramFrequency);

		WriteFinalFile(outputPath, finalBigramPath, bigramCount, finalTrigramPath, trigramCount, tempDir);

		foreach (var f in bigramChunkPaths) File.Delete(f);
		foreach (var f in trigramChunkPaths) File.Delete(f);
		File.Delete(finalBigramPath); File.Delete(finalTrigramPath);
	}

	private static void ParseFile(string path, CompiledDictionary dictionary, ConcurrentDictionary<string, bool> validCache, Dictionary<ulong, int> bigrams, Dictionary<ulong, int> trigrams)
	{
		using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 64 * 1024);
		using var reader = new StreamReader(stream);

		var validHashes = new List<ulong>(128);

		string? line;
		while ((line = reader.ReadLine()) != null)
		{
			var words = line.Split(new[] { ' ', '.', ',', '!', '?', ';', ':', '(', ')', '"', '-', '—', '\t' }, StringSplitOptions.RemoveEmptyEntries);

			validHashes.Clear();

			for (int i = 0; i < words.Length; i++)
			{
				string lowerWord = words[i].ToLowerInvariant();

				bool isValid = validCache.GetOrAdd(lowerWord, w => dictionary.Analyze(w).Any());

				if (isValid)
				{
					validHashes.Add(MurmurHash3.Hash(lowerWord));
				}
				else
				{
					ProcessWindow(validHashes, bigrams, trigrams);
					validHashes.Clear();
				}
			}
			ProcessWindow(validHashes, bigrams, trigrams);
		}
	}

	private static void ProcessWindow(List<ulong> hashes, Dictionary<ulong, int> bigrams, Dictionary<ulong, int> trigrams)
	{
		if (hashes.Count < 2) return;

		for (int i = 0; i < hashes.Count - 1; i++)
		{
			ulong bHash = CombineHashes(hashes[i], hashes[i + 1]) & 0x0000FFFFFFFFFFFFUL;
			ref int val = ref CollectionsMarshal.GetValueRefOrAddDefault(bigrams, bHash, out _);
			val++;

			if (i < hashes.Count - 2)
			{
				ulong tHash = CombineHashes(bHash, hashes[i + 2]) & 0x0000FFFFFFFFFFFFUL;
				ref int tVal = ref CollectionsMarshal.GetValueRefOrAddDefault(trigrams, tHash, out _);
				tVal++;
			}
		}
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

	private int MergeChunks(List<string> chunkPaths, string outputPath, int minFreq)
	{
		var readers = new BinaryReader[chunkPaths.Count];
		var priorityQueue = new PriorityQueue<(ulong Hash, int Count, int ReaderIndex), ulong>();

		for (int i = 0; i < chunkPaths.Count; i++)
		{
			var fs = new FileStream(chunkPaths[i], FileMode.Open, FileAccess.Read, FileShare.Read, 64 * 1024);
			readers[i] = new BinaryReader(fs);
			if (fs.Length > 0)
			{
				ulong hash = readers[i].ReadUInt64();
				int count = readers[i].ReadInt32();
				priorityQueue.Enqueue((hash, count, i), hash);
			}
			else
			{
				readers[i].Dispose();
				readers[i] = null!;
			}
		}

		using var outFs = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None, 64 * 1024);
		using var writer = new BinaryWriter(outFs);

		ulong currentHash = 0;
		int currentTotal = 0;
		bool isFirst = true;
		int uniqueWritten = 0;
		const double AssumedMaxLog = 16.11;

		void Flush()
		{
			if (!isFirst && currentTotal >= minFreq)
			{
				double logCurrent = Math.Log(currentTotal + 1);
				byte score = (byte)Math.Round((logCurrent / AssumedMaxLog) * 255.0);
				score = Math.Max((byte)1, score);

				writer.Write(currentHash);
				writer.Write(score);
				uniqueWritten++;
			}
		}

		while (priorityQueue.Count > 0)
		{
			var (hash, count, idx) = priorityQueue.Dequeue();

			if (isFirst)
			{
				currentHash = hash;
				currentTotal = count;
				isFirst = false;
			}
			else if (hash == currentHash)
			{
				currentTotal += count;
			}
			else
			{
				Flush();
				currentHash = hash;
				currentTotal = count;
			}

			var reader = readers[idx];
			if (reader != null && reader.BaseStream.Position < reader.BaseStream.Length)
			{
				ulong nextHash = reader.ReadUInt64();
				int nextCount = reader.ReadInt32();
				priorityQueue.Enqueue((nextHash, nextCount, idx), nextHash);
			}
			else
			{
				reader?.Dispose();
				readers[idx] = null!;
			}
		}
		Flush();

		foreach (var r in readers) r?.Dispose();
		return uniqueWritten;
	}

	private void WriteFinalFile(string finalPath, string bigramPath, int bigramCount, string trigramPath, int trigramCount, string tempDir)
	{
		using var fileStream = new FileStream(finalPath, FileMode.Create, FileAccess.Write);
		using var deflateStream = new DeflateStream(fileStream, CompressionLevel.SmallestSize, true);
		using var writer = new BinaryWriter(deflateStream);

		writer.Write(0x4E47524D);
		writer.Write((ushort)1);
		writer.Write(bigramCount);
		writer.Write(trigramCount);

		TransferAndBucketizeStreaming(bigramPath, bigramCount, writer, tempDir);
		TransferAndBucketizeStreaming(trigramPath, trigramCount, writer, tempDir);
	}

	private void TransferAndBucketizeStreaming(string sourcePath, int count, BinaryWriter mainWriter, string tempDir)
	{
		int[] buckets = new int[65537];
		int currentBucket = 0;
		buckets[0] = 0;

		using var sourceFs = new FileStream(sourcePath, FileMode.Open, FileAccess.Read);
		using var sourceReader = new BinaryReader(sourceFs);

		string scoreTempPath = Path.Combine(tempDir, $"scores_{Guid.NewGuid()}.tmp");
		using var scoreFs = new FileStream(scoreTempPath, FileMode.Create, FileAccess.ReadWrite);
		using var scoreWriter = new BinaryWriter(scoreFs);

		for (int i = 0; i < count; i++)
		{
			ulong hash48 = sourceReader.ReadUInt64();
			sourceReader.ReadByte();

			int bucket = (int)(hash48 >> 32);
			while (currentBucket < bucket)
			{
				currentBucket++;
				buckets[currentBucket] = i;
			}
		}
		while (currentBucket < 65536)
		{
			currentBucket++;
			buckets[currentBucket] = count;
		}

		var bucketBytes = new byte[buckets.Length * sizeof(int)];
		Buffer.BlockCopy(buckets, 0, bucketBytes, 0, bucketBytes.Length);
		mainWriter.Write(bucketBytes);

		sourceFs.Position = 0;

		for (int i = 0; i < count; i++)
		{
			ulong hash48 = sourceReader.ReadUInt64();
			byte score = sourceReader.ReadByte();

			uint hash32 = (uint)(hash48 & 0xFFFFFFFF);
			mainWriter.Write(hash32);

			scoreWriter.Write(score);
		}

		scoreFs.Position = 0;
		scoreFs.CopyTo(mainWriter.BaseStream);

		scoreWriter.Dispose();
		File.Delete(scoreTempPath);
	}

	private static ulong CombineHashes(ulong seed, ulong value)
	{
		return seed ^ (value + 0x9e3779b97f4a7c15UL + (seed << 6) + (seed >> 2));
	}
}