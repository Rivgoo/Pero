using Pero.Abstractions.Contracts;
using Pero.Abstractions.Models.Morphology;
using Pero.Kernel.Dictionaries.Models;
using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;

namespace Pero.Kernel.Dictionaries;

public class FstSuffixDictionary<TTag> where TTag : MorphologicalTag
{
	public byte[] FstData { get; private set; } = Array.Empty<byte>();
	public FlatMorphologyRule[] Rules { get; private set; } = Array.Empty<FlatMorphologyRule>();
	public TTag[] Tagsets { get; private set; } = Array.Empty<TTag>();

	private FlatMorphologyRule[] reverseRules = Array.Empty<FlatMorphologyRule>();
	private ushort[][] paradigms = Array.Empty<ushort[]>();
	private char[] suffixPool = Array.Empty<char>();
	private byte[] lemmaFstData = Array.Empty<byte>();

	public void Load(Stream inputStream, IMorphologyDecoder<TTag> decoder)
	{
		using var deflateStream = new DeflateStream(inputStream, CompressionMode.Decompress, true);
		using var reader = new BinaryReader(deflateStream, Encoding.UTF8);

		var magic = reader.ReadUInt32();
		if (magic != BinaryDictionaryHeader.MagicNumber) throw new InvalidDataException("Invalid dictionary format.");

		reader.ReadUInt16(); // Version
		var tagsBlobSize = reader.ReadUInt32();
		var rulesCount = reader.ReadUInt32();

		uint reverseRulesCount = reader.ReadUInt32();
		uint paradigmsCount = reader.ReadUInt32();
		uint fstSize = reader.ReadUInt32();
		uint lemmaFstSize = reader.ReadUInt32();

		var tagsBlob = reader.ReadBytes((int)tagsBlobSize);
		Tagsets = decoder.Decode(tagsBlob);

		var suffixPoolLength = reader.ReadUInt32();
		suffixPool = Encoding.UTF8.GetChars(reader.ReadBytes((int)suffixPoolLength));

		Rules = ReadRules(reader, rulesCount);
		reverseRules = ReadRules(reader, reverseRulesCount);

		paradigms = new ushort[paradigmsCount][];
		for (int i = 0; i < paradigmsCount; i++)
		{
			ushort len = reader.ReadUInt16();
			paradigms[i] = new ushort[len];
			for (int j = 0; j < len; j++) paradigms[i][j] = reader.ReadUInt16();
		}

		FstData = reader.ReadBytes((int)fstSize);
		lemmaFstData = reader.ReadBytes((int)lemmaFstSize);
	}

	/// <summary>
	/// Blazingly fast lookup. Traverses the FST using a Span. 
	/// Zero allocations. Bails out immediately on the first mismatched character.
	/// </summary>
	public bool Contains(ReadOnlySpan<char> word)
	{
		if (FstData.Length == 0) return false;
		uint currentOffset = 0;

		foreach (var c in word)
		{
			byte flags = FstData[(int)currentOffset];
			byte arcCount = FstData[(int)currentOffset + 1];
			int ptr = (int)currentOffset + 2;

			if ((flags & 0x02) != 0)
			{
				ptr += 1;
				ushort ruleCount = BinaryPrimitives.ReadUInt16LittleEndian(FstData.AsSpan(ptr));
				ptr += 2 + (ruleCount * 2);
			}

			bool found = false;
			for (int i = 0; i < arcCount; i++)
			{
				char transitionChar = (char)BinaryPrimitives.ReadUInt16LittleEndian(FstData.AsSpan(ptr));
				if (transitionChar == c)
				{
					currentOffset = BinaryPrimitives.ReadUInt32LittleEndian(FstData.AsSpan(ptr + 2));
					found = true;
					break;
				}
				ptr += 6;
			}
			if (!found) return false;
		}

		byte finalFlags = FstData[(int)currentOffset];
		return (finalFlags & 0x01) != 0 && (finalFlags & 0x02) != 0;
	}

	public bool Contains(string word) => Contains(word.AsSpan());

	/// <summary>
	/// Extracts frequency and tag references without resolving the full morphology rules (No strings created).
	/// Perfect for Spell-Check candidate ranking.
	/// </summary>
	public bool TryGetFrequencyAndTags(ReadOnlySpan<char> word, out byte frequency, out TTag[] tags)
	{
		frequency = 0;
		tags = Array.Empty<TTag>();
		if (FstData.Length == 0) return false;

		uint currentOffset = 0;

		foreach (var c in word)
		{
			byte flags = FstData[(int)currentOffset];
			byte arcCount = FstData[(int)currentOffset + 1];
			int ptr = (int)currentOffset + 2;

			if ((flags & 0x02) != 0)
			{
				ptr += 1;
				ushort ruleCount = BinaryPrimitives.ReadUInt16LittleEndian(FstData.AsSpan(ptr));
				ptr += 2 + (ruleCount * 2);
			}

			bool found = false;
			for (int i = 0; i < arcCount; i++)
			{
				char transitionChar = (char)BinaryPrimitives.ReadUInt16LittleEndian(FstData.AsSpan(ptr));
				if (transitionChar == c)
				{
					currentOffset = BinaryPrimitives.ReadUInt32LittleEndian(FstData.AsSpan(ptr + 2));
					found = true;
					break;
				}
				ptr += 6;
			}
			if (!found) return false;
		}

		byte finalFlags = FstData[(int)currentOffset];
		if ((finalFlags & 0x01) == 0 || (finalFlags & 0x02) == 0) return false;

		int payloadPtr = (int)currentOffset + 2;
		frequency = FstData[payloadPtr];
		payloadPtr += 1;

		ushort finalRuleCount = BinaryPrimitives.ReadUInt16LittleEndian(FstData.AsSpan(payloadPtr));
		payloadPtr += 2;

		if (finalRuleCount > 0)
		{
			tags = new TTag[finalRuleCount];
			for (int i = 0; i < finalRuleCount; i++)
			{
				ushort ruleId = BinaryPrimitives.ReadUInt16LittleEndian(FstData.AsSpan(payloadPtr));
				payloadPtr += 2;
				tags[i] = Tagsets[Rules[ruleId].TagId];
			}
		}

		return true;
	}

	public IEnumerable<MorphologicalInfo> Analyze(string word)
	{
		if (FstData.Length == 0) yield break;
		uint currentOffset = 0;

		foreach (var c in word)
		{
			byte flags = FstData[(int)currentOffset];
			byte arcCount = FstData[(int)currentOffset + 1];
			int ptr = (int)currentOffset + 2;

			if ((flags & 0x02) != 0)
			{
				ptr += 1;
				ushort ruleCount = BinaryPrimitives.ReadUInt16LittleEndian(FstData.AsSpan(ptr));
				ptr += 2 + (ruleCount * 2);
			}

			bool found = false;
			for (int i = 0; i < arcCount; i++)
			{
				char transitionChar = (char)BinaryPrimitives.ReadUInt16LittleEndian(FstData.AsSpan(ptr));
				if (transitionChar == c)
				{
					currentOffset = BinaryPrimitives.ReadUInt32LittleEndian(FstData.AsSpan(ptr + 2));
					found = true;
					break;
				}
				ptr += 6;
			}
			if (!found) yield break;
		}

		byte finalFlags = FstData[(int)currentOffset];
		if ((finalFlags & 0x01) == 0 || (finalFlags & 0x02) == 0) yield break;

		int payloadPtr = (int)currentOffset + 2 + 1;
		ushort finalRuleCount = BinaryPrimitives.ReadUInt16LittleEndian(FstData.AsSpan(payloadPtr));
		payloadPtr += 2;

		for (int i = 0; i < finalRuleCount; i++)
		{
			ushort ruleId = BinaryPrimitives.ReadUInt16LittleEndian(FstData.AsSpan(payloadPtr));
			payloadPtr += 2;

			var rule = Rules[ruleId];
			yield return new MorphologicalInfo(ApplyRule(word, rule), Tagsets[rule.TagId]);
		}
	}

	public IEnumerable<WordForm<TTag>> GetAllForms(string lemma)
	{
		if (lemmaFstData.Length == 0) yield break;
		uint currentOffset = 0;

		foreach (var c in lemma)
		{
			byte flags = lemmaFstData[(int)currentOffset];
			byte arcCount = lemmaFstData[(int)currentOffset + 1];
			int ptr = (int)currentOffset + 2;

			if ((flags & 0x02) != 0) ptr += 2;

			bool found = false;
			for (int i = 0; i < arcCount; i++)
			{
				char transitionChar = (char)BinaryPrimitives.ReadUInt16LittleEndian(lemmaFstData.AsSpan(ptr));
				if (transitionChar == c)
				{
					currentOffset = BinaryPrimitives.ReadUInt32LittleEndian(lemmaFstData.AsSpan(ptr + 2));
					found = true;
					break;
				}
				ptr += 6;
			}
			if (!found) yield break;
		}

		byte finalFlags = lemmaFstData[(int)currentOffset];
		if ((finalFlags & 0x01) == 0 || (finalFlags & 0x02) == 0) yield break;

		ushort paradigmId = BinaryPrimitives.ReadUInt16LittleEndian(lemmaFstData.AsSpan((int)currentOffset + 2));

		foreach (var ruleId in paradigms[paradigmId])
		{
			var rule = reverseRules[ruleId];
			yield return new WordForm<TTag>(ApplyRule(lemma, rule), Tagsets[rule.TagId]);
		}
	}

	private string ApplyRule(string source, FlatMorphologyRule rule)
	{
		if (rule.CutLength > source.Length) return source;

		int prefixLen = source.Length - rule.CutLength;
		int totalLen = prefixLen + rule.SuffixLength;
		var state = (Source: source, Rule: rule, Pool: suffixPool, PrefixLen: prefixLen);

		return string.Create(totalLen, state, static (span, st) =>
		{
			st.Source.AsSpan(0, st.PrefixLen).CopyTo(span);
			var suffixSpan = new ReadOnlySpan<char>(st.Pool, (int)st.Rule.SuffixOffset, st.Rule.SuffixLength);
			suffixSpan.CopyTo(span.Slice(st.PrefixLen));
		});
	}

	private static FlatMorphologyRule[] ReadRules(BinaryReader reader, uint count)
	{
		if (count == 0) return Array.Empty<FlatMorphologyRule>();

		var rules = new FlatMorphologyRule[count];
		for (int i = 0; i < count; i++)
		{
			rules[i] = new FlatMorphologyRule(
				cutLength: reader.ReadByte(),
				suffixOffset: reader.ReadUInt32(),
				suffixLength: reader.ReadByte(),
				tagId: reader.ReadUInt16()
			);
		}
		return rules;
	}
}