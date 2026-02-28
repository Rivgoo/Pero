namespace Pero.Kernel.Dictionaries.Models;

public class ForwardPayload : IEquatable<ForwardPayload>
{
	public byte Frequency { get; }
	public ushort[] RuleIds { get; }

	public ForwardPayload(byte frequency, ushort[] ruleIds)
	{
		Frequency = frequency;
		RuleIds = ruleIds;
	}

	public bool Equals(ForwardPayload? other)
	{
		if (other is null) return false;
		if (ReferenceEquals(this, other)) return true;
		return Frequency == other.Frequency && RuleIds.SequenceEqual(other.RuleIds);
	}

	public override bool Equals(object? obj) => Equals(obj as ForwardPayload);

	public override int GetHashCode()
	{
		var hash = new HashCode();
		hash.Add(Frequency);
		foreach (var id in RuleIds) hash.Add(id);
		return hash.ToHashCode();
	}
}