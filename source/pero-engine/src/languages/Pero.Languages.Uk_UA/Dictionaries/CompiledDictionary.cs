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
	private char[] _suffixPool = Array.Empty<char>();
	private byte[] _fstData = Array.Empty<byte>();

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
		var fstSize = reader.ReadUInt32();

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
		var suffixBytes = reader.ReadBytes((int)suffixPoolLength);
		_suffixPool = Encoding.UTF8.GetChars(suffixBytes);

		_rules = new FlatMorphologyRule[rulesCount];
		for (int i = 0; i < rulesCount; i++)
		{
			_rules[i] = new FlatMorphologyRule(
				cutLength: reader.ReadByte(),
				suffixOffset: reader.ReadUInt32(),
				suffixLength: reader.ReadByte(),
				tagId: reader.ReadUInt16()
			);
		}

		_fstData = reader.ReadBytes((int)fstSize);
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

			bool hasPayload = (flags & 0x02) != 0;
			if (hasPayload)
			{
				ptr += 1; // Freq
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
		bool isFinal = (finalFlags & 0x01) != 0;
		bool finalHasPayload = (finalFlags & 0x02) != 0;

		if (!isFinal || !finalHasPayload) yield break;

		int payloadPtr = (int)currentOffset + 2;

		payloadPtr += 1; // Freq

		ushort finalRuleCount = BinaryPrimitives.ReadUInt16LittleEndian(_fstData.AsSpan(payloadPtr));
		payloadPtr += 2;

		for (int i = 0; i < finalRuleCount; i++)
		{
			ushort ruleId = BinaryPrimitives.ReadUInt16LittleEndian(_fstData.AsSpan(payloadPtr));
			payloadPtr += 2;

			var rule = _rules[ruleId];
			var tagset = _tagsets[rule.TagId];
			var lemma = ApplyRule(word, rule);
			yield return new MorphologicalInfo(lemma, tagset);
		}
	}

	private string ApplyRule(string form, FlatMorphologyRule rule)
	{
		if (rule.CutLength > form.Length) return form;
		int prefixLen = form.Length - rule.CutLength;
		int totalLen = prefixLen + rule.SuffixLength;
		var state = (Form: form, Rule: rule, Pool: _suffixPool, PrefixLen: prefixLen);

		return string.Create(totalLen, state, static (span, st) =>
		{
			st.Form.AsSpan(0, st.PrefixLen).CopyTo(span);
			var suffixSpan = new ReadOnlySpan<char>(st.Pool, (int)st.Rule.SuffixOffset, st.Rule.SuffixLength);
			suffixSpan.CopyTo(span.Slice(st.PrefixLen));
		});
	}
}