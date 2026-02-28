using System.Runtime.InteropServices;
using System.Text;

namespace Pero.Kernel.Utils;

public static class MurmurHash3
{
	private const ulong C1 = 0x87c37b91114253d5L;
	private const ulong C2 = 0x4cf5ad432745937fL;

	public static ulong Hash(string text)
	{
		return Hash(text.AsSpan());
	}

	/// <summary>
	/// Generates a hash directly from a Span, avoiding string allocations.
	/// </summary>
	public static ulong Hash(ReadOnlySpan<char> text)
	{
		// A word is rarely longer than 30 chars. UTF-8 max is 3 bytes per Cyrillic char.
		// 128 chars * 3 = 384 bytes, perfectly safe for stackalloc.
		int maxBytes = text.Length * 3;
		Span<byte> bytes = maxBytes <= 512 ? stackalloc byte[maxBytes] : new byte[maxBytes];

		int byteCount = Encoding.UTF8.GetBytes(text, bytes);
		return Hash(bytes.Slice(0, byteCount));
	}

	public static ulong Hash(ReadOnlySpan<byte> data)
	{
		ulong h1 = 0;
		ulong h2 = 0;
		int length = data.Length;
		int blocks = length / 16;

		ReadOnlySpan<ulong> blockArray = MemoryMarshal.Cast<byte, ulong>(data);

		for (int i = 0; i < blocks; i++)
		{
			ulong k1 = blockArray[i * 2];
			ulong k2 = blockArray[i * 2 + 1];

			k1 *= C1;
			k1 = RotateLeft(k1, 31);
			k1 *= C2;
			h1 ^= k1;

			h1 = RotateLeft(h1, 27);
			h1 += h2;
			h1 = h1 * 5 + 0x52dce729;

			k2 *= C2;
			k2 = RotateLeft(k2, 33);
			k2 *= C1;
			h2 ^= k2;

			h2 = RotateLeft(h2, 31);
			h2 += h1;
			h2 = h2 * 5 + 0x38495ab5;
		}

		int tailIndex = blocks * 16;
		ulong tailK1 = 0;
		ulong tailK2 = 0;

		switch (length & 15)
		{
			case 15: tailK2 ^= (ulong)data[tailIndex + 14] << 48; goto case 14;
			case 14: tailK2 ^= (ulong)data[tailIndex + 13] << 40; goto case 13;
			case 13: tailK2 ^= (ulong)data[tailIndex + 12] << 32; goto case 12;
			case 12: tailK2 ^= (ulong)data[tailIndex + 11] << 24; goto case 11;
			case 11: tailK2 ^= (ulong)data[tailIndex + 10] << 16; goto case 10;
			case 10: tailK2 ^= (ulong)data[tailIndex + 9] << 8; goto case 9;
			case 9:
				tailK2 ^= data[tailIndex + 8];
				tailK2 *= C2;
				tailK2 = RotateLeft(tailK2, 33);
				tailK2 *= C1;
				h2 ^= tailK2;
				goto case 8;
			case 8: tailK1 ^= (ulong)data[tailIndex + 7] << 56; goto case 7;
			case 7: tailK1 ^= (ulong)data[tailIndex + 6] << 48; goto case 6;
			case 6: tailK1 ^= (ulong)data[tailIndex + 5] << 40; goto case 5;
			case 5: tailK1 ^= (ulong)data[tailIndex + 4] << 32; goto case 4;
			case 4: tailK1 ^= (ulong)data[tailIndex + 3] << 24; goto case 3;
			case 3: tailK1 ^= (ulong)data[tailIndex + 2] << 16; goto case 2;
			case 2: tailK1 ^= (ulong)data[tailIndex + 1] << 8; goto case 1;
			case 1:
				tailK1 ^= data[tailIndex];
				tailK1 *= C1;
				tailK1 = RotateLeft(tailK1, 31);
				tailK1 *= C2;
				h1 ^= tailK1;
				break;
		}

		h1 ^= (ulong)length;
		h2 ^= (ulong)length;

		h1 += h2;
		h2 += h1;

		h1 = FMix(h1);
		h2 = FMix(h2);

		h1 += h2;
		h2 += h1;

		return h1;
	}

	private static ulong RotateLeft(ulong x, int r) => (x << r) | (x >> (64 - r));

	private static ulong FMix(ulong k)
	{
		k ^= k >> 33;
		k *= 0xff51afd7ed558ccdL;
		k ^= k >> 33;
		k *= 0xc4ceb9fe1a85ec53L;
		k ^= k >> 33;
		return k;
	}
}