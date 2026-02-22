using System.Runtime.InteropServices;

namespace Pero.Languages.Uk_UA.Dictionaries.Models;

/// <summary>
/// The fixed-size header for the binary dictionary format.
/// Allows rapid verification and offset calculation for zero-copy reads.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct BinaryDictionaryHeader
{
	public const uint MagicNumber = 0x5045524F; // "PERO" in ASCII
	public const ushort CurrentVersion = 1;

	public uint Magic { get; }
	public ushort Version { get; }
	public uint TagsetsCount { get; }
	public uint RulesCount { get; }
	public uint FstSize { get; }

	public BinaryDictionaryHeader(uint tagsetsCount, uint rulesCount, uint fstSize)
	{
		Magic = MagicNumber;
		Version = CurrentVersion;
		TagsetsCount = tagsetsCount;
		RulesCount = rulesCount;
		FstSize = fstSize;
	}
}