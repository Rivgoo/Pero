using System.IO.Compression;
using System.Runtime.InteropServices;

namespace Pero.Languages.Uk_UA.Dictionaries.Ngrams;

public class NgramLanguageModel
{
	private const uint MagicNumber = 0x4E47524D; // "NGRM"
	private const ushort Version = 1;

	private int[] _bigramBuckets = Array.Empty<int>();
	private uint[] _bigramHashes = Array.Empty<uint>();
	private byte[] _bigramScores = Array.Empty<byte>();

	private int[] _trigramBuckets = Array.Empty<int>();
	private uint[] _trigramHashes = Array.Empty<uint>();
	private byte[] _trigramScores = Array.Empty<byte>();

	public void Load(Stream inputStream)
	{
		using var deflateStream = new DeflateStream(inputStream, CompressionMode.Decompress, true);
		using var reader = new BinaryReader(deflateStream);

		if (reader.ReadUInt32() != MagicNumber) throw new InvalidDataException("Invalid N-gram format.");
		if (reader.ReadUInt16() != Version) throw new InvalidDataException("Unsupported N-gram version.");

		int bigramCount = reader.ReadInt32();
		int trigramCount = reader.ReadInt32();

		_bigramBuckets = ReadArray<int>(reader, 65537);
		_bigramHashes = ReadArray<uint>(reader, bigramCount);
		_bigramScores = reader.ReadBytes(bigramCount);

		_trigramBuckets = ReadArray<int>(reader, 65537);
		_trigramHashes = ReadArray<uint>(reader, trigramCount);
		_trigramScores = reader.ReadBytes(trigramCount);
	}

	public byte GetBigramScore(ulong word1Hash, ulong word2Hash)
	{
		if (_bigramHashes.Length == 0) return 0;
		return GetScore(CombineHashes(word1Hash, word2Hash), _bigramBuckets, _bigramHashes, _bigramScores);
	}

	public byte GetTrigramScore(ulong word1Hash, ulong word2Hash, ulong word3Hash)
	{
		if (_trigramHashes.Length == 0) return 0;
		return GetScore(CombineHashes(CombineHashes(word1Hash, word2Hash), word3Hash), _trigramBuckets, _trigramHashes, _trigramScores);
	}

	private static byte GetScore(ulong fullHash, int[] buckets, uint[] hashes, byte[] scores)
	{
		// Extract 48-bit fingerprint
		ulong hash48 = fullHash & 0x0000FFFFFFFFFFFFUL;

		// Top 16 bits for bucket
		int bucket = (int)(hash48 >> 32);

		// Lower 32 bits for binary search
		uint targetHash32 = (uint)(hash48 & 0xFFFFFFFF);

		int start = buckets[bucket];
		int length = buckets[bucket + 1] - start;

		if (length == 0) return 0;

		int index = Array.BinarySearch(hashes, start, length, targetHash32);
		return index >= 0 ? scores[index] : (byte)0;
	}

	private static ulong CombineHashes(ulong seed, ulong value)
	{
		return seed ^ (value + 0x9e3779b97f4a7c15UL + (seed << 6) + (seed >> 2));
	}

	private static T[] ReadArray<T>(BinaryReader reader, int count) where T : unmanaged
	{
		if (count == 0) return Array.Empty<T>();
		int byteSize = count * Marshal.SizeOf<T>();
		var bytes = reader.ReadBytes(byteSize);
		var result = new T[count];
		Buffer.BlockCopy(bytes, 0, result, 0, byteSize);
		return result;
	}
}