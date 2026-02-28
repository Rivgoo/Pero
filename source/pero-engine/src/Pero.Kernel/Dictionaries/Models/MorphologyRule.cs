namespace Pero.Kernel.Dictionaries.Models;

/// <summary>
/// Represents a transformation rule to reconstruct a lemma from a word form.
/// Used during FST traversal.
/// </summary>
public readonly struct MorphologyRule : IEquatable<MorphologyRule>
{
	/// <summary>
	/// Number of characters to remove from the end of the word form.
	/// </summary>
	public byte CutLength { get; }

	/// <summary>
	/// The suffix to append after cutting to form the lemma.
	/// </summary>
	public string AddSuffix { get; }

	/// <summary>
	/// The ID of the morphological tagset associated with this rule.
	/// Maps to an array index in the Tagset section of the binary dictionary.
	/// </summary>
	public ushort TagId { get; }

	public MorphologyRule(byte cutLength, string addSuffix, ushort tagId)
	{
		CutLength = cutLength;
		AddSuffix = addSuffix;
		TagId = tagId;
	}

	public bool Equals(MorphologyRule other)
	{
		return CutLength == other.CutLength && AddSuffix == other.AddSuffix && TagId == other.TagId;
	}

	public override bool Equals(object? obj) => obj is MorphologyRule other && Equals(other);

	public override int GetHashCode() => HashCode.Combine(CutLength, AddSuffix, TagId);
}