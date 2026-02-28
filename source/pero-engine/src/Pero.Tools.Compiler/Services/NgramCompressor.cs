using System.IO.Compression;

namespace Pero.Tools.Compiler.Services;

public class NgramCompressor
{
	public int MergeChunks(List<string> chunkPaths, string outputPath, int minFreq)
	{
		var readers = new BinaryReader[chunkPaths.Count];
		var priorityQueue = new PriorityQueue<(ulong Hash, int Count, int ReaderIndex), ulong>();

		for (int i = 0; i < chunkPaths.Count; i++)
		{
			var fs = new FileStream(chunkPaths[i], FileMode.Open, FileAccess.Read, FileShare.Read, 64 * 1024);
			readers[i] = new BinaryReader(fs);
			if (fs.Length > 0)
			{
				ulong hash = readers[i].ReadUInt64();
				int count = readers[i].ReadInt32();
				priorityQueue.Enqueue((hash, count, i), hash);
			}
			else
			{
				readers[i].Dispose();
				readers[i] = null!;
			}
		}

		using var outFs = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None, 64 * 1024);
		using var writer = new BinaryWriter(outFs);

		ulong currentHash = 0;
		int currentTotal = 0;
		bool isFirst = true;
		int uniqueWritten = 0;
		const double AssumedMaxLog = 16.11;

		void Flush()
		{
			if (!isFirst && currentTotal >= minFreq)
			{
				double logCurrent = Math.Log(currentTotal + 1);
				byte score = (byte)Math.Round((logCurrent / AssumedMaxLog) * 255.0);
				score = Math.Max((byte)1, score);

				writer.Write(currentHash);
				writer.Write(score);
				uniqueWritten++;
			}
		}

		while (priorityQueue.Count > 0)
		{
			var (hash, count, idx) = priorityQueue.Dequeue();

			if (isFirst)
			{
				currentHash = hash;
				currentTotal = count;
				isFirst = false;
			}
			else if (hash == currentHash)
			{
				currentTotal += count;
			}
			else
			{
				Flush();
				currentHash = hash;
				currentTotal = count;
			}

			var reader = readers[idx];
			if (reader != null && reader.BaseStream.Position < reader.BaseStream.Length)
			{
				ulong nextHash = reader.ReadUInt64();
				int nextCount = reader.ReadInt32();
				priorityQueue.Enqueue((nextHash, nextCount, idx), nextHash);
			}
			else
			{
				reader?.Dispose();
				readers[idx] = null!;
			}
		}
		Flush();

		foreach (var r in readers) r?.Dispose();
		return uniqueWritten;
	}

	public void WriteFinalFile(string finalPath, string bigramPath, int bigramCount, string trigramPath, int trigramCount, string tempDir)
	{
		using var fileStream = new FileStream(finalPath, FileMode.Create, FileAccess.Write);
		using var deflateStream = new DeflateStream(fileStream, CompressionLevel.SmallestSize, true);
		using var writer = new BinaryWriter(deflateStream);

		writer.Write(0x4E47524D); // NGRM
		writer.Write((ushort)1);
		writer.Write(bigramCount);
		writer.Write(trigramCount);

		TransferAndBucketizeStreaming(bigramPath, bigramCount, writer, tempDir);
		TransferAndBucketizeStreaming(trigramPath, trigramCount, writer, tempDir);
	}

	private void TransferAndBucketizeStreaming(string sourcePath, int count, BinaryWriter mainWriter, string tempDir)
	{
		int[] buckets = new int[65537];
		int currentBucket = 0;
		buckets[0] = 0;

		using var sourceFs = new FileStream(sourcePath, FileMode.Open, FileAccess.Read);
		using var sourceReader = new BinaryReader(sourceFs);

		string scoreTempPath = Path.Combine(tempDir, $"scores_{Guid.NewGuid()}.tmp");
		using var scoreFs = new FileStream(scoreTempPath, FileMode.Create, FileAccess.ReadWrite);
		using var scoreWriter = new BinaryWriter(scoreFs);

		for (int i = 0; i < count; i++)
		{
			ulong hash48 = sourceReader.ReadUInt64();
			sourceReader.ReadByte();

			int bucket = (int)(hash48 >> 32);
			while (currentBucket < bucket)
			{
				currentBucket++;
				buckets[currentBucket] = i;
			}
		}
		while (currentBucket < 65536)
		{
			currentBucket++;
			buckets[currentBucket] = count;
		}

		var bucketBytes = new byte[buckets.Length * sizeof(int)];
		Buffer.BlockCopy(buckets, 0, bucketBytes, 0, bucketBytes.Length);
		mainWriter.Write(bucketBytes);

		sourceFs.Position = 0;

		for (int i = 0; i < count; i++)
		{
			ulong hash48 = sourceReader.ReadUInt64();
			byte score = sourceReader.ReadByte();
			uint hash32 = (uint)(hash48 & 0xFFFFFFFF);

			mainWriter.Write(hash32);
			scoreWriter.Write(score);
		}

		scoreFs.Position = 0;
		scoreFs.CopyTo(mainWriter.BaseStream);

		scoreWriter.Dispose();
		File.Delete(scoreTempPath);
	}
}