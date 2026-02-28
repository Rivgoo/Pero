using System.Text;
using Pero.Kernel.Dictionaries.Models;
using Pero.Tools.Compiler.Contracts;
using Pero.Tools.Compiler.Models;

namespace Pero.Tools.Compiler.Services;

public class FstSuffixDictionaryParser
{
	private readonly IMorphologyCompilerPlugin compilerPlugin;

	public FstSuffixDictionaryParser(IMorphologyCompilerPlugin compilerPlugin)
	{
		this.compilerPlugin = compilerPlugin;
	}

	public FstSuffixDictionaryBuildData Parse(IEnumerable<string> rawLines, IReadOnlyDictionary<string, byte>? frequencies)
	{
		var data = new FstSuffixDictionaryBuildData();
		var ruleRegistry = new Dictionary<FlatMorphologyRule, ushort>();
		var reverseRuleRegistry = new Dictionary<FlatMorphologyRule, ushort>();

		foreach (var line in rawLines)
		{
			if (string.IsNullOrWhiteSpace(line)) continue;

			var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
			if (parts.Length < 3) continue;

			var form = parts[0];
			var lemma = parts[1];
			var tagString = parts[2];

			var tagId = compilerPlugin.GetOrAddTagId(tagString);

			var fRule = RuleGenerator.Generate(form, lemma, tagId);
			var fFlat = CreateFlatRule(fRule, data.SuffixPool, data.SuffixRegistry);
			if (!ruleRegistry.TryGetValue(fFlat, out var fRuleId))
			{
				fRuleId = (ushort)data.Rules.Count;
				ruleRegistry[fFlat] = fRuleId;
				data.Rules.Add(fFlat);
			}

			if (!data.FormMap.TryGetValue(form, out var fRules)) data.FormMap[form] = (fRules = new List<ushort>());
			fRules.Add(fRuleId);

			var rRule = RuleGenerator.Generate(lemma, form, tagId);
			var rFlat = CreateFlatRule(rRule, data.SuffixPool, data.SuffixRegistry);
			if (!reverseRuleRegistry.TryGetValue(rFlat, out var rRuleId))
			{
				rRuleId = (ushort)data.ReverseRules.Count;
				reverseRuleRegistry[rFlat] = rRuleId;
				data.ReverseRules.Add(rFlat);
			}

			if (!data.LemmaMap.TryGetValue(lemma, out var rRules)) data.LemmaMap[lemma] = (rRules = new List<ushort>());
			rRules.Add(rRuleId);
		}

		data.TagsBlob = compilerPlugin.SerializeTags();
		ProcessParadigms(data);
		BuildTrees(data, frequencies);

		return data;
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

	private void ProcessParadigms(FstSuffixDictionaryBuildData data)
	{
		var paradigmRegistry = new Dictionary<string, ushort>(StringComparer.Ordinal);
		data.LemmaEntries = new List<(string Lemma, LemmaPayload Payload)>(data.LemmaMap.Count);

		foreach (var kvp in data.LemmaMap)
		{
			var uniqueRules = kvp.Value.Distinct().Order().ToArray();
			var pKey = string.Join(",", uniqueRules);

			if (!paradigmRegistry.TryGetValue(pKey, out var pId))
			{
				pId = (ushort)data.Paradigms.Count;
				paradigmRegistry[pKey] = pId;
				data.Paradigms.Add(uniqueRules);
			}
			data.LemmaEntries.Add((kvp.Key, new LemmaPayload(pId)));
		}
	}

	private void BuildTrees(FstSuffixDictionaryBuildData data, IReadOnlyDictionary<string, byte>? frequencies)
	{
		var forwardEntries = new List<(string Form, ForwardPayload Payload)>(data.FormMap.Count);
		foreach (var kvp in data.FormMap)
		{
			byte freq = frequencies != null && frequencies.TryGetValue(kvp.Key, out var f) ? f : (byte)0;
			var uniqueRules = kvp.Value.Distinct().ToArray();
			forwardEntries.Add((kvp.Key, new ForwardPayload(freq, uniqueRules)));
		}

		forwardEntries.Sort((a, b) => string.CompareOrdinal(a.Form, b.Form));
		var forwardBuilder = new DawgBuilder<ForwardPayload>((a, b) =>
			new ForwardPayload(Math.Max(a.Frequency, b.Frequency), a.RuleIds.Concat(b.RuleIds).Distinct().ToArray()));

		foreach (var entry in forwardEntries) forwardBuilder.Insert(entry.Form, entry.Payload);
		data.ForwardRoot = forwardBuilder.Finish();
		CalculateMaxSubtreeFrequencies(data.ForwardRoot, new HashSet<FstNode<ForwardPayload>>());

		data.LemmaEntries.Sort((a, b) => string.CompareOrdinal(a.Lemma, b.Lemma));
		var lemmaBuilder = new DawgBuilder<LemmaPayload>((a, b) => a);

		foreach (var entry in data.LemmaEntries) lemmaBuilder.Insert(entry.Lemma, entry.Payload);
		data.LemmaRoot = lemmaBuilder.Finish();
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
}