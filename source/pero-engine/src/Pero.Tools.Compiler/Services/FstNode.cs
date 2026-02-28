namespace Pero.Tools.Compiler.Services;

public class FstNode<TPayload> : IEquatable<FstNode<TPayload>> where TPayload : IEquatable<TPayload>
{
	public Dictionary<char, FstNode<TPayload>> Arcs { get; } = new();
	public bool IsFinal { get; set; }
	public TPayload? Payload { get; set; }
	public byte MaxFrequencyInSubtree { get; set; } = 0;

	public bool Equals(FstNode<TPayload>? other)
	{
		if (other is null) return false;
		if (ReferenceEquals(this, other)) return true;

		if (IsFinal != other.IsFinal) return false;
		if (!EqualityComparer<TPayload?>.Default.Equals(Payload, other.Payload)) return false;
		if (Arcs.Count != other.Arcs.Count) return false;

		foreach (var kvp in Arcs)
		{
			if (!other.Arcs.TryGetValue(kvp.Key, out var otherTarget)) return false;
			if (!ReferenceEquals(kvp.Value, otherTarget)) return false;
		}
		return true;
	}

	public override bool Equals(object? obj) => Equals(obj as FstNode<TPayload>);

	public override int GetHashCode()
	{
		var hash = new HashCode();
		hash.Add(IsFinal);
		hash.Add(Payload);
		foreach (var kvp in Arcs)
		{
			hash.Add(kvp.Key);
			hash.Add(kvp.Value.GetHashCode());
		}
		return hash.ToHashCode();
	}
}