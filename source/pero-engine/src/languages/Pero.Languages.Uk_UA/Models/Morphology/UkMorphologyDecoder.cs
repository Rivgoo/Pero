using Pero.Abstractions.Contracts;

namespace Pero.Languages.Uk_UA.Models.Morphology;

public class UkMorphologyDecoder : IMorphologyDecoder<UkMorphologyTag>
{
	// 8 bytes (single byte enums) + 2 bytes (ushort Features)
	private const int SerializedTagSize = 10;

	public UkMorphologyTag[] Decode(byte[] data)
	{
		int count = data.Length / SerializedTagSize;
		var tags = new UkMorphologyTag[count];

		using var ms = new MemoryStream(data);
		using var reader = new BinaryReader(ms);

		for (int i = 0; i < count; i++)
		{
			tags[i] = new UkMorphologyTag(
				(PartOfSpeech)reader.ReadByte(),
				(GrammarCase)reader.ReadByte(),
				(GrammarGender)reader.ReadByte(),
				(GrammarNumber)reader.ReadByte(),
				(GrammarAnimacy)reader.ReadByte(),
				(GrammarAspect)reader.ReadByte(),
				(GrammarTense)reader.ReadByte(),
				(GrammarPerson)reader.ReadByte(),
				(GrammarFeatures)reader.ReadUInt16()
			);
		}

		return tags;
	}
}