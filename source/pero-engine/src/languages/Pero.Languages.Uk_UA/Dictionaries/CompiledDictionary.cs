using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;
using Pero.Abstractions.Models;
using Pero.Abstractions.Models.Morphology;
using Pero.Languages.Uk_UA.Dictionaries.Models;

namespace Pero.Languages.Uk_UA.Dictionaries;

public class CompiledDictionary
{
	public byte[] FstData => _fstData;
	public FlatMorphologyRule[] Rules => _rules;
	public MorphologyTagset[] Tagsets => _tagsets;

	private MorphologyTagset[] _tagsets = Array.Empty<MorphologyTagset>();
	private FlatMorphologyRule[] _rules = Array.Empty<FlatMorphologyRule>();
	private FlatMorphologyRule[] _reverseRules = Array.Empty<FlatMorphologyRule>();
	private ushort[][] _paradigms = Array.Empty<ushort[]>();
	private char[] _suffixPool = Array.Empty<char>();

	private byte[] _fstData = Array.Empty<byte>();
	private byte[] _lemmaFstData = Array.Empty<byte>();

	public void Load(Stream inputStream)
	{
		using var deflateStream = new DeflateStream(inputStream, CompressionMode.Decompress, true);
		using var reader = new BinaryReader(deflateStream, Encoding.UTF8);

		var magic = reader.ReadUInt32();
		if (magic != BinaryDictionaryHeader.MagicNumber)
			throw new InvalidDataException("Invalid dictionary format.");

		var version = reader.ReadUInt16();
		var tagsetsCount = reader.ReadUInt32();
		var rulesCount = reader.ReadUInt32();

		uint reverseRulesCount = 0;
		uint paradigmsCount = 0;
		uint fstSize = 0;
		uint lemmaFstSize = 0;

		reverseRulesCount = reader.ReadUInt32();
		paradigmsCount = reader.ReadUInt32();
		fstSize = reader.ReadUInt32();
		lemmaFstSize = reader.ReadUInt32();

		_tagsets = new MorphologyTagset[tagsetsCount];
		for (int i = 0; i < tagsetsCount; i++)
		{
			_tagsets[i] = new MorphologyTagset(
				(PartOfSpeech)reader.ReadByte(),
				(GrammarCase)reader.ReadByte(),
				(GrammarGender)reader.ReadByte(),
				(GrammarNumber)reader.ReadByte(),
				(GrammarAnimacy)reader.ReadByte(),
				(GrammarAspect)reader.ReadByte(),
				(GrammarTense)reader.ReadByte(),
				(GrammarPerson)reader.ReadByte(),
				(GrammarFeatures)reader.ReadUInt16()
			);
		}

		var suffixPoolLength = reader.ReadUInt32();
		_suffixPool = Encoding.UTF8.GetChars(reader.ReadBytes((int)suffixPoolLength));

		_rules = ReadRules(reader, rulesCount);

		_reverseRules = ReadRules(reader, reverseRulesCount);

		_paradigms = new ushort[paradigmsCount][];
		for (int i = 0; i < paradigmsCount; i++)
		{
			ushort len = reader.ReadUInt16();
			_paradigms[i] = new ushort[len];
			for (int j = 0; j < len; j++) _paradigms[i][j] = reader.ReadUInt16();
		}

		_fstData = reader.ReadBytes((int)fstSize);

		_lemmaFstData = reader.ReadBytes((int)lemmaFstSize);
	}

	public IEnumerable<MorphologicalInfo> Analyze(string word)
	{
		if (_fstData.Length == 0) yield break;
		uint currentOffset = 0;

		foreach (var c in word)
		{
			byte flags = _fstData[(int)currentOffset];
			byte arcCount = _fstData[(int)currentOffset + 1];
			int ptr = (int)currentOffset + 2;

			if ((flags & 0x02) != 0)
			{
				ptr += 1;
				ushort ruleCount = BinaryPrimitives.ReadUInt16LittleEndian(_fstData.AsSpan(ptr));
				ptr += 2 + (ruleCount * 2);
			}

			bool found = false;
			for (int i = 0; i < arcCount; i++)
			{
				char transitionChar = (char)BinaryPrimitives.ReadUInt16LittleEndian(_fstData.AsSpan(ptr));
				if (transitionChar == c)
				{
					currentOffset = BinaryPrimitives.ReadUInt32LittleEndian(_fstData.AsSpan(ptr + 2));
					found = true;
					break;
				}
				ptr += 6;
			}
			if (!found) yield break;
		}

		byte finalFlags = _fstData[(int)currentOffset];
		if ((finalFlags & 0x01) == 0 || (finalFlags & 0x02) == 0) yield break;

		int payloadPtr = (int)currentOffset + 2 + 1;
		ushort finalRuleCount = BinaryPrimitives.ReadUInt16LittleEndian(_fstData.AsSpan(payloadPtr));
		payloadPtr += 2;

		for (int i = 0; i < finalRuleCount; i++)
		{
			ushort ruleId = BinaryPrimitives.ReadUInt16LittleEndian(_fstData.AsSpan(payloadPtr));
			payloadPtr += 2;

			var rule = _rules[ruleId];
			yield return new MorphologicalInfo(ApplyRule(word, rule), _tagsets[rule.TagId]);
		}
	}

	public IEnumerable<WordForm> GetAllForms(string lemma)
	{
		if (_lemmaFstData.Length == 0) yield break;
		uint currentOffset = 0;

		foreach (var c in lemma)
		{
			byte flags = _lemmaFstData[(int)currentOffset];
			byte arcCount = _lemmaFstData[(int)currentOffset + 1];
			int ptr = (int)currentOffset + 2;

			if ((flags & 0x02) != 0) ptr += 2;

			bool found = false;
			for (int i = 0; i < arcCount; i++)
			{
				char transitionChar = (char)BinaryPrimitives.ReadUInt16LittleEndian(_lemmaFstData.AsSpan(ptr));
				if (transitionChar == c)
				{
					currentOffset = BinaryPrimitives.ReadUInt32LittleEndian(_lemmaFstData.AsSpan(ptr + 2));
					found = true;
					break;
				}
				ptr += 6;
			}
			if (!found) yield break;
		}

		byte finalFlags = _lemmaFstData[(int)currentOffset];
		if ((finalFlags & 0x01) == 0 || (finalFlags & 0x02) == 0) yield break;

		ushort paradigmId = BinaryPrimitives.ReadUInt16LittleEndian(_lemmaFstData.AsSpan((int)currentOffset + 2));

		foreach (var ruleId in _paradigms[paradigmId])
		{
			var rule = _reverseRules[ruleId];
			yield return new WordForm(ApplyRule(lemma, rule), _tagsets[rule.TagId]);
		}
	}

	private string ApplyRule(string source, FlatMorphologyRule rule)
	{
		if (rule.CutLength > source.Length) return source;

		int prefixLen = source.Length - rule.CutLength;
		int totalLen = prefixLen + rule.SuffixLength;
		var state = (Source: source, Rule: rule, Pool: _suffixPool, PrefixLen: prefixLen);

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