using Pero.Languages.Uk_UA.Dictionaries.Models;

namespace Pero.Languages.Uk_UA.Dictionaries.Builder;

/// <summary>
/// Represents a state/node in the FST during the construction phase.
/// Will be compiled down to a flat byte array for production use.
/// </summary>
public class FstNode : IEquatable<FstNode>
{
	public Dictionary<char, FstNode> Arcs { get; } = new();
	public bool IsFinal { get; set; }
	public FstPayload? Payload { get; set; }

	/// <summary>
	/// Compares the structural equivalence of two nodes to perform suffix merging.
	/// </summary>
	public bool Equals(FstNode? other)
	{
		if (other is null) return false;
		if (ReferenceEquals(this, other)) return true;

		if (IsFinal != other.IsFinal) return false;

		if (!EqualityComparer<FstPayload?>.Default.Equals(Payload, other.Payload))
			return false;

		if (Arcs.Count != other.Arcs.Count) return false;

		foreach (var kvp in Arcs)
		{
			if (!other.Arcs.TryGetValue(kvp.Key, out var otherTarget)) return false;
			if (!ReferenceEquals(kvp.Value, otherTarget)) return false; // Reference equality for children is required for DAG validation
		}

		return true;
	}

	public override bool Equals(object? obj) => Equals(obj as FstNode);

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