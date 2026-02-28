using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using Pero.Kernel.Dictionaries;
using Pero.Kernel.Utils;

namespace Pero.Tools.Compiler.Services;

public class NgramCounter
{
	public void ProcessFile(
		string path,
		CompiledDictionary dictionary,
		ConcurrentDictionary<string, bool> validCache,
		Dictionary<ulong, int> bigrams,
		Dictionary<ulong, int> trigrams)
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
					AccumulateHashes(validHashes, bigrams, trigrams);
					validHashes.Clear();
				}
			}
			AccumulateHashes(validHashes, bigrams, trigrams);
		}
	}

	private void AccumulateHashes(List<ulong> hashes, Dictionary<ulong, int> bigrams, Dictionary<ulong, int> trigrams)
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

	private static ulong CombineHashes(ulong seed, ulong value) =>
		seed ^ (value + 0x9e3779b97f4a7c15UL + (seed << 6) + (seed >> 2));
}