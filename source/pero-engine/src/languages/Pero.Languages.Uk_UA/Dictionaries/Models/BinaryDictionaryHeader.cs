using System.Runtime.InteropServices;

namespace Pero.Languages.Uk_UA.Dictionaries.Models;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct BinaryDictionaryHeader
{
	public const uint MagicNumber = 0x5045524F; // "PERO" in ASCII
	public const ushort CurrentVersion = 1;

	public uint Magic { get; }
	public ushort Version { get; }
	public uint TagsetsCount { get; }
	public uint RulesCount { get; }
	public uint ReverseRulesCount { get; }
	public uint ParadigmsCount { get; }
	public uint FstSize { get; }
	public uint LemmaFstSize { get; }

	public BinaryDictionaryHeader(
		uint tagsetsCount,
		uint rulesCount,
		uint reverseRulesCount,
		uint paradigmsCount,
		uint fstSize,
		uint lemmaFstSize)
	{
		Magic = MagicNumber;
		Version = CurrentVersion;
		TagsetsCount = tagsetsCount;
		RulesCount = rulesCount;
		ReverseRulesCount = reverseRulesCount;
		ParadigmsCount = paradigmsCount;
		FstSize = fstSize;
		LemmaFstSize = lemmaFstSize;
	}
}