using System.IO.Compression;
using System.Text;
using Pero.Kernel.Dictionaries.Models;

namespace Pero.Tools.Compiler.Services;

public class FstSerializer
{
	public void WriteFlatBinary(
		Stream outputStream, byte[] tagsBlob,
		List<FlatMorphologyRule> rules, List<FlatMorphologyRule> reverseRules,
		List<ushort[]> paradigms, string suffixPool,
		FstNode<ForwardPayload> forwardRoot, FstNode<LemmaPayload> lemmaRoot)
	{
		using var deflateStream = new DeflateStream(outputStream, CompressionLevel.SmallestSize, true);
		using var writer = new BinaryWriter(deflateStream, Encoding.UTF8, leaveOpen: true);

		byte[] forwardData = SerializeForwardFst(forwardRoot);
		byte[] lemmaData = SerializeLemmaFst(lemmaRoot);

		var header = new BinaryDictionaryHeader(
			(uint)tagsBlob.Length, (uint)rules.Count, (uint)reverseRules.Count,
			(uint)paradigms.Count, (uint)forwardData.Length, (uint)lemmaData.Length);

		writer.Write(header.Magic);
		writer.Write(header.Version);
		writer.Write(header.TagsBlobSize);
		writer.Write(header.RulesCount);
		writer.Write(header.ReverseRulesCount);
		writer.Write(header.ParadigmsCount);
		writer.Write(header.FstSize);
		writer.Write(header.LemmaFstSize);

		writer.Write(tagsBlob);

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
				if (node.IsFinal && node.Payload != null) currentOffset += 2;
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