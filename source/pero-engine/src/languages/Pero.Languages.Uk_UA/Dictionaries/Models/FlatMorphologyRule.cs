using System.Runtime.InteropServices;

namespace Pero.Languages.Uk_UA.Dictionaries.Models;

/// <summary>
/// A zero-allocation, highly optimized rule structure.
/// References a global string pool instead of holding string objects.
/// Size: 8 bytes.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct FlatMorphologyRule
{
	public byte CutLength { get; }
	public uint SuffixOffset { get; }
	public byte SuffixLength { get; }
	public ushort TagId { get; }

	public FlatMorphologyRule(byte cutLength, uint suffixOffset, byte suffixLength, ushort tagId)
	{
		CutLength = cutLength;
		SuffixOffset = suffixOffset;
		SuffixLength = suffixLength;
		TagId = tagId;
	}
}