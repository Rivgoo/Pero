namespace Pero.Languages.Uk_UA.Dictionaries.Models;

/// <summary>
/// The data stored in an accepting (final) state of the FST.
/// </summary>
public class FstPayload : IEquatable<FstPayload>
{
	/// <summary>
	/// A frequency score from a text corpus (0-255). Higher is more frequent.
	/// Used to sort spellchecking suggestions.
	/// </summary>
	public byte Frequency { get; }

	/// <summary>
	/// A list of rule IDs applied to this word form.
	/// Multiple IDs exist because a word form can be homonymous (e.g., different cases, same spelling).
	/// </summary>
	public ushort[] RuleIds { get; }

	public FstPayload(byte frequency, ushort[] ruleIds)
	{
		Frequency = frequency;
		RuleIds = ruleIds;
	}

	public bool Equals(FstPayload? other)
	{
		if (other is null) return false;
		if (ReferenceEquals(this, other)) return true;
		return Frequency == other.Frequency && RuleIds.SequenceEqual(other.RuleIds);
	}

	public override bool Equals(object? obj) => Equals(obj as FstPayload);

	public override int GetHashCode()
	{
		var hash = new HashCode();
		hash.Add(Frequency);
		foreach (var id in RuleIds) hash.Add(id);
		return hash.ToHashCode();
	}
}