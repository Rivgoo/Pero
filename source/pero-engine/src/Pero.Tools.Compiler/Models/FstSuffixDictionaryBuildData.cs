using Pero.Kernel.Dictionaries.Models;
using Pero.Tools.Compiler.Services;
using System.Text;

namespace Pero.Tools.Compiler.Models;

public class FstSuffixDictionaryBuildData
{
	public byte[] TagsBlob { get; set; } = Array.Empty<byte>();
	public StringBuilder SuffixPool { get; } = new();
	public Dictionary<string, (uint Offset, byte Length)> SuffixRegistry { get; } = new();
	public List<FlatMorphologyRule> Rules { get; } = new();
	public List<FlatMorphologyRule> ReverseRules { get; } = new();
	public Dictionary<string, List<ushort>> FormMap { get; } = new(StringComparer.Ordinal);
	public Dictionary<string, List<ushort>> LemmaMap { get; } = new(StringComparer.Ordinal);
	public List<ushort[]> Paradigms { get; } = new();
	public List<(string Lemma, LemmaPayload Payload)> LemmaEntries { get; set; } = new();
	public FstNode<ForwardPayload>? ForwardRoot { get; set; }
	public FstNode<LemmaPayload>? LemmaRoot { get; set; }
}