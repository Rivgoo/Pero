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
		var reverseRuleRegistry = new Dictionary<FlatMorphologyRule, ushort>();
		var ruleList = new List<FlatMorphologyRule>();
		var reverseRuleList = new List<FlatMorphologyRule>();

		var formMap = new Dictionary<string, List<ushort>>(StringComparer.Ordinal);
		var lemmaMap = new Dictionary<string, List<ushort>>(StringComparer.Ordinal);

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

			// Forward (Form -> Lemma)
			var fRule = RuleGenerator.Generate(form, lemma, tagId);
			var fFlat = CreateFlatRule(fRule, suffixPoolBuilder, suffixRegistry);
			if (!ruleRegistry.TryGetValue(fFlat, out var fRuleId))
			{
				fRuleId = (ushort)ruleList.Count;
				ruleRegistry[fFlat] = fRuleId;
				ruleList.Add(fFlat);
			}

			if (!formMap.TryGetValue(form, out var fRules)) formMap[form] = (fRules = new List<ushort>());
			fRules.Add(fRuleId);

			// Reverse (Lemma -> Form)
			var rRule = RuleGenerator.Generate(lemma, form, tagId);
			var rFlat = CreateFlatRule(rRule, suffixPoolBuilder, suffixRegistry);
			if (!reverseRuleRegistry.TryGetValue(rFlat, out var rRuleId))
			{
				rRuleId = (ushort)reverseRuleList.Count;
				reverseRuleRegistry[rFlat] = rRuleId;
				reverseRuleList.Add(rFlat);
			}

			if (!lemmaMap.TryGetValue(lemma, out var rRules)) lemmaMap[lemma] = (rRules = new List<ushort>());
			rRules.Add(rRuleId);
		}

		var paradigmRegistry = new Dictionary<string, ushort>(StringComparer.Ordinal);
		var paradigmsList = new List<ushort[]>();

		var lemmaEntries = new List<(string Lemma, LemmaPayload Payload)>(lemmaMap.Count);
		foreach (var kvp in lemmaMap)
		{
			var uniqueRules = kvp.Value.Distinct().Order().ToArray();
			var pKey = string.Join(",", uniqueRules);

			if (!paradigmRegistry.TryGetValue(pKey, out var pId))
			{
				pId = (ushort)paradigmsList.Count;
				paradigmRegistry[pKey] = pId;
				paradigmsList.Add(uniqueRules);
			}
			lemmaEntries.Add((kvp.Key, new LemmaPayload(pId)));
		}

		var forwardEntries = new List<(string Form, ForwardPayload Payload)>(formMap.Count);
		foreach (var kvp in formMap)
		{
			byte freq = frequencies != null && frequencies.TryGetValue(kvp.Key, out var f) ? f : (byte)0;
			var uniqueRules = kvp.Value.Distinct().ToArray();
			forwardEntries.Add((kvp.Key, new ForwardPayload(freq, uniqueRules)));
		}

		forwardEntries.Sort((a, b) => string.CompareOrdinal(a.Form, b.Form));
		var forwardBuilder = new DawgBuilder<ForwardPayload>((a, b) =>
			new ForwardPayload(Math.Max(a.Frequency, b.Frequency), a.RuleIds.Concat(b.RuleIds).Distinct().ToArray()));

		foreach (var entry in forwardEntries) forwardBuilder.Insert(entry.Form, entry.Payload);
		var forwardRoot = forwardBuilder.Finish();
		CalculateMaxSubtreeFrequencies(forwardRoot, new HashSet<FstNode<ForwardPayload>>());

		lemmaEntries.Sort((a, b) => string.CompareOrdinal(a.Lemma, b.Lemma));
		var lemmaBuilder = new DawgBuilder<LemmaPayload>((a, b) => a);

		foreach (var entry in lemmaEntries) lemmaBuilder.Insert(entry.Lemma, entry.Payload);
		var lemmaRoot = lemmaBuilder.Finish();

		WriteFlatBinary(
			outputStream, tagsetList, ruleList, reverseRuleList, paradigmsList,
			suffixPoolBuilder.ToString(), forwardRoot, lemmaRoot);
	}

	private FlatMorphologyRule CreateFlatRule(MorphologyRule rule, StringBuilder pool, Dictionary<string, (uint Offset, byte Length)> registry)
	{
		if (!registry.TryGetValue(rule.AddSuffix, out var info))
		{
			info = ((uint)pool.Length, (byte)rule.AddSuffix.Length);
			pool.Append(rule.AddSuffix);
			registry[rule.AddSuffix] = info;
		}
		return new FlatMorphologyRule(rule.CutLength, info.Offset, info.Length, rule.TagId);
	}

	private void CalculateMaxSubtreeFrequencies(FstNode<ForwardPayload> node, HashSet<FstNode<ForwardPayload>> visited)
	{
		if (!visited.Add(node)) return;

		byte maxFreq = node.Payload?.Frequency ?? 0;
		foreach (var child in node.Arcs.Values)
		{
			CalculateMaxSubtreeFrequencies(child, visited);
			if (child.MaxFrequencyInSubtree > maxFreq) maxFreq = child.MaxFrequencyInSubtree;
		}
		node.MaxFrequencyInSubtree = maxFreq;
	}

	private void WriteFlatBinary(
		Stream outputStream, List<MorphologyTagset> tagsets,
		List<FlatMorphologyRule> rules, List<FlatMorphologyRule> reverseRules,
		List<ushort[]> paradigms, string suffixPool,
		FstNode<ForwardPayload> forwardRoot, FstNode<LemmaPayload> lemmaRoot)
	{
		// Використовуємо LeaveOpen=true, щоб уникнути закриття базового потоку
		using var deflateStream = new DeflateStream(outputStream, CompressionLevel.SmallestSize, true);
		using var writer = new BinaryWriter(deflateStream, Encoding.UTF8, leaveOpen: true);

		byte[] forwardData = SerializeForwardFst(forwardRoot);
		byte[] lemmaData = SerializeLemmaFst(lemmaRoot);

		var header = new BinaryDictionaryHeader(
			(uint)tagsets.Count, (uint)rules.Count, (uint)reverseRules.Count,
			(uint)paradigms.Count, (uint)forwardData.Length, (uint)lemmaData.Length);

		writer.Write(header.Magic);
		writer.Write(header.Version);
		writer.Write(header.TagsetsCount);
		writer.Write(header.RulesCount);
		writer.Write(header.ReverseRulesCount);
		writer.Write(header.ParadigmsCount);
		writer.Write(header.FstSize);
		writer.Write(header.LemmaFstSize);

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

		WriteRules(writer, rules);
		WriteRules(writer, reverseRules);

		foreach (var paradigm in paradigms)
		{
			writer.Write((ushort)paradigm.Length);
			foreach (var id in paradigm) writer.Write(id);
		}

		writer.Write(forwardData);
		writer.Write(lemmaData);

		writer.Flush();
	}

	private void WriteRules(BinaryWriter writer, List<FlatMorphologyRule> rules)
	{
		foreach (var r in rules)
		{
			writer.Write(r.CutLength);
			writer.Write(r.SuffixOffset);
			writer.Write(r.SuffixLength);
			writer.Write(r.TagId);
		}
	}

	private byte[] SerializeForwardFst(FstNode<ForwardPayload> root)
	{
		using var ms = new MemoryStream();
		using (var writer = new BinaryWriter(ms, Encoding.UTF8, leaveOpen: true))
		{
			var nodeList = new List<FstNode<ForwardPayload>>();
			var nodeOffsets = new Dictionary<FstNode<ForwardPayload>, uint>();
			uint currentOffset = 0;

			void CollectNodes(FstNode<ForwardPayload> node)
			{
				if (nodeOffsets.ContainsKey(node)) return;
				nodeOffsets[node] = 0;
				nodeList.Add(node);
				var sortedArcs = node.Arcs.OrderByDescending(kvp => kvp.Value.MaxFrequencyInSubtree).ThenBy(kvp => kvp.Key).ToList();
				foreach (var arc in sortedArcs) CollectNodes(arc.Value);
			}

			CollectNodes(root);

			foreach (var node in nodeList)
			{
				nodeOffsets[node] = currentOffset;
				currentOffset += 2;
				if (node.IsFinal && node.Payload != null)
				{
					currentOffset += 3;
					currentOffset += (uint)(node.Payload.RuleIds.Length * 2);
				}
				currentOffset += (uint)(node.Arcs.Count * 6);
			}

			foreach (var node in nodeList)
			{
				byte flags = (byte)((node.IsFinal ? 0x01 : 0) | (node.Payload != null ? 0x02 : 0));
				writer.Write(flags);
				writer.Write((byte)node.Arcs.Count);

				if (node.IsFinal && node.Payload != null)
				{
					writer.Write(node.Payload.Frequency);
					writer.Write((ushort)node.Payload.RuleIds.Length);
					foreach (var id in node.Payload.RuleIds) writer.Write(id);
				}

				var sortedArcs = node.Arcs.OrderByDescending(kvp => kvp.Value.MaxFrequencyInSubtree).ThenBy(kvp => kvp.Key);
				foreach (var arc in sortedArcs)
				{
					writer.Write((ushort)arc.Key);
					writer.Write(nodeOffsets[arc.Value]);
				}
			}
			writer.Flush();
		}
		return ms.ToArray();
	}

	private byte[] SerializeLemmaFst(FstNode<LemmaPayload> root)
	{
		using var ms = new MemoryStream();
		using (var writer = new BinaryWriter(ms, Encoding.UTF8, leaveOpen: true))
		{
			var nodeList = new List<FstNode<LemmaPayload>>();
			var nodeOffsets = new Dictionary<FstNode<LemmaPayload>, uint>();
			uint currentOffset = 0;

			void CollectNodes(FstNode<LemmaPayload> node)
			{
				if (nodeOffsets.ContainsKey(node)) return;
				nodeOffsets[node] = 0;
				nodeList.Add(node);
				foreach (var arc in node.Arcs.OrderBy(kvp => kvp.Key)) CollectNodes(arc.Value);
			}

			CollectNodes(root);

			foreach (var node in nodeList)
			{
				nodeOffsets[node] = currentOffset;
				currentOffset += 2;
				if (node.IsFinal && node.Payload != null) currentOffset += 2; // ParadigmId
				currentOffset += (uint)(node.Arcs.Count * 6);
			}

			foreach (var node in nodeList)
			{
				byte flags = (byte)((node.IsFinal ? 0x01 : 0) | (node.Payload != null ? 0x02 : 0));
				writer.Write(flags);
				writer.Write((byte)node.Arcs.Count);

				if (node.IsFinal && node.Payload != null)
				{
					writer.Write(node.Payload.ParadigmId);
				}

				foreach (var arc in node.Arcs.OrderBy(kvp => kvp.Key))
				{
					writer.Write((ushort)arc.Key);
					writer.Write(nodeOffsets[arc.Value]);
				}
			}
			writer.Flush();
		}
		return ms.ToArray();
	}
}