using System.Runtime.InteropServices;
using Pero.Abstractions.Models.Morphology;
using Pero.Kernel.Dictionaries;
using Pero.Kernel.Utils;

namespace Pero.Tools.Compiler.Services;

public class NgramCounter<TTag> where TTag : MorphologicalTag
{
	public void ProcessFile(
		string path,
		FstSuffixDictionary<TTag> dictionary,
		Dictionary<ulong, int> bigrams,
		Dictionary<ulong, int> trigrams)
	{
		using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 1024 * 1024);
		using var reader = new StreamReader(stream);

		var validHashes = new List<ulong>(128);
		Span<char> wordBuffer = stackalloc char[128];

		string? line;
		while ((line = reader.ReadLine()) != null)
		{
			validHashes.Clear();
			int length = 0;
			var lineSpan = line.AsSpan();

			for (int i = 0; i <= lineSpan.Length; i++)
			{
				bool isWordChar = false;
				char c = '\0';

				if (i < lineSpan.Length)
				{
					c = lineSpan[i];
					isWordChar = char.IsLetter(c) || IsApostrophe(c);
				}

				if (isWordChar)
				{
					if (length < 128)
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

						if (dictionary.Contains(wordSpan))
						{
							validHashes.Add(MurmurHash3.Hash(wordSpan));
						}
						else
						{
							AccumulateHashes(validHashes, bigrams, trigrams);
							validHashes.Clear();
						}
					}
					length = 0;
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

	private static bool IsApostrophe(char c) => c == '\'' || c == '’' || c == 'ʼ';

	private static ulong CombineHashes(ulong seed, ulong value) =>
		seed ^ (value + 0x9e3779b97f4a7c15UL + (seed << 6) + (seed >> 2));
}