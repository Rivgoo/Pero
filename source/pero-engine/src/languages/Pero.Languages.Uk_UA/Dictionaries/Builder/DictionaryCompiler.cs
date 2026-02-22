using System.IO.Compression;
using System.Text;
using Pero.Abstractions.Models.Morphology;
using Pero.Languages.Uk_UA.Dictionaries.Models;
using Pero.Languages.Uk_UA.Dictionaries.Parsing;

namespace Pero.Languages.Uk_UA.Dictionaries.Builder;

public class DictionaryCompiler
{
	public void Compile(IEnumerable<string> rawDictionaryLines, Stream outputStream, IReadOnlyDictionary<string, byte>? frequencies = null)
	{
		var tagsetRegistry = new Dictionary<MorphologyTagset, ushort>();
		var tagsetList = new List<MorphologyTagset>();

		var suffixPoolBuilder = new StringBuilder();
		var suffixRegistry = new Dictionary<string, (uint Offset, byte Length)>();

		var ruleRegistry = new Dictionary<FlatMorphologyRule, ushort>();
		var ruleList = new List<FlatMorphologyRule>();

		var entries = new List<(string Form, ushort RuleId)>();

		foreach (var line in rawDictionaryLines)
		{
			if (string.IsNullOrWhiteSpace(line)) continue;

			var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
			if (parts.Length < 3) continue;

			var form = parts[0];
			var lemma = parts[1];
			var tagString = parts[2];

			var tagset = UkTagParser.Parse(tagString);
			if (!tagsetRegistry.TryGetValue(tagset, out var tagId))
			{
				tagId = (ushort)tagsetList.Count;
				tagsetRegistry[tagset] = tagId;
				tagsetList.Add(tagset);
			}

			var tempRule = RuleGenerator.Generate(form, lemma, tagId);

			if (!suffixRegistry.TryGetValue(tempRule.AddSuffix, out var suffixInfo))
			{
				suffixInfo = ((uint)suffixPoolBuilder.Length, (byte)tempRule.AddSuffix.Length);
				suffixPoolBuilder.Append(tempRule.AddSuffix);
				suffixRegistry[tempRule.AddSuffix] = suffixInfo;
			}

			var flatRule = new FlatMorphologyRule(tempRule.CutLength, suffixInfo.Offset, suffixInfo.Length, tagId);

			if (!ruleRegistry.TryGetValue(flatRule, out var ruleId))
			{
				ruleId = (ushort)ruleList.Count;
				ruleRegistry[flatRule] = ruleId;
				ruleList.Add(flatRule);
			}

			entries.Add((form, ruleId));
		}

		entries.Sort((a, b) => string.CompareOrdinal(a.Form, b.Form));

		var builder = new DawgBuilder();
		string currentForm = string.Empty;
		var currentRuleIds = new List<ushort>();

		foreach (var entry in entries)
		{
			if (entry.Form != currentForm && currentForm != string.Empty)
			{
				InsertIntoBuilder(builder, currentForm, currentRuleIds, frequencies);
			}
			currentForm = entry.Form;
			currentRuleIds.Add(entry.RuleId);
		}

		if (currentForm != string.Empty)
		{
			InsertIntoBuilder(builder, currentForm, currentRuleIds, frequencies);
		}

		var rootNode = builder.Finish();
		string suffixPool = suffixPoolBuilder.ToString();

		WriteFlatBinary(outputStream, tagsetList, ruleList, suffixPool, rootNode);
	}

	private void InsertIntoBuilder(DawgBuilder builder, string form, List<ushort> ruleIds, IReadOnlyDictionary<string, byte>? frequencies)
	{
		byte freq = 0;
		if (frequencies != null && frequencies.TryGetValue(form, out var foundFreq))
		{
			freq = foundFreq;
		}

		builder.Insert(form, new FstPayload(freq, ruleIds.ToArray()));
		ruleIds.Clear();
	}

	private void WriteFlatBinary(Stream outputStream, List<MorphologyTagset> tagsets, List<FlatMorphologyRule> rules, string suffixPool, FstNode root)
	{
		using var brotliStream = new BrotliStream(outputStream, CompressionLevel.SmallestSize, true);
		using var writer = new BinaryWriter(brotliStream, Encoding.UTF8);

		var nodeList = new List<FstNode>();
		var nodeOffsets = new Dictionary<FstNode, uint>();
		uint currentOffset = 0;

		void CollectNodes(FstNode node)
		{
			if (nodeOffsets.ContainsKey(node)) return;

			nodeOffsets[node] = 0;
			nodeList.Add(node);

			foreach (var child in node.Arcs.Values) CollectNodes(child);
		}

		CollectNodes(root);

		foreach (var node in nodeList)
		{
			nodeOffsets[node] = currentOffset;
			currentOffset += 2;

			if (node.IsFinal && node.Payload != null)
			{
				currentOffset += 1;
				currentOffset += 2;
				currentOffset += (uint)(node.Payload.RuleIds.Length * 2);
			}

			currentOffset += (uint)(node.Arcs.Count * 6);
		}

		var header = new BinaryDictionaryHeader((uint)tagsets.Count, (uint)rules.Count, currentOffset);

		writer.Write(header.Magic);
		writer.Write(header.Version);
		writer.Write(header.TagsetsCount);
		writer.Write(header.RulesCount);
		writer.Write(header.FstSize);

		foreach (var t in tagsets)
		{
			writer.Write((byte)t.PartOfSpeech);
			writer.Write((byte)t.Case);
			writer.Write((byte)t.Gender);
			writer.Write((byte)t.Number);
			writer.Write((byte)t.Animacy);
			writer.Write((byte)t.Aspect);
			writer.Write((byte)t.Tense);
			writer.Write((byte)t.Person);
			writer.Write((ushort)t.Features);
		}

		var suffixBytes = Encoding.UTF8.GetBytes(suffixPool);
		writer.Write((uint)suffixBytes.Length);
		writer.Write(suffixBytes);

		foreach (var r in rules)
		{
			writer.Write(r.CutLength);
			writer.Write(r.SuffixOffset);
			writer.Write(r.SuffixLength);
			writer.Write(r.TagId);
		}

		foreach (var node in nodeList)
		{
			byte flags = 0;
			if (node.IsFinal) flags |= 0x01;
			if (node.Payload != null) flags |= 0x02;

			writer.Write(flags);
			writer.Write((byte)node.Arcs.Count);

			if (node.IsFinal && node.Payload != null)
			{
				writer.Write(node.Payload.Frequency);
				writer.Write((ushort)node.Payload.RuleIds.Length);
				foreach (var id in node.Payload.RuleIds) writer.Write(id);
			}

			foreach (var arc in node.Arcs)
			{
				writer.Write((ushort)arc.Key);
				writer.Write(nodeOffsets[arc.Value]);
			}
		}
	}
}