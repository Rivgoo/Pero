namespace Pero.Languages.Uk_UA.Dictionaries;

internal class RuntimeFstNode
{
	public bool IsFinal { get; }
	public byte Frequency { get; }
	public ushort[] RuleIds { get; }
	public Dictionary<char, uint> Arcs { get; }

	public RuntimeFstNode(bool isFinal, byte frequency, ushort[] ruleIds, Dictionary<char, uint> arcs)
	{
		IsFinal = isFinal;
		Frequency = frequency;
		RuleIds = ruleIds;
		Arcs = arcs;
	}
}