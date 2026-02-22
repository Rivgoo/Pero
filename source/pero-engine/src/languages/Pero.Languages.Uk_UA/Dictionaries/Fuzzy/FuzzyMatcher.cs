using Pero.Abstractions.Models.Morphology;
using Pero.Languages.Uk_UA.Dictionaries.Models;
using System.Buffers.Binary;

namespace Pero.Languages.Uk_UA.Dictionaries.Fuzzy;

public partial class FuzzyMatcher
{
	private const int MaxResults = 15;
	private const int MaxWordLength = 64;

	private byte[] _fstData => _dictionary.FstData;
	private FlatMorphologyRule[] _rules => _dictionary.Rules;
	private MorphologyTagset[] _tagsets => _dictionary.Tagsets;

	private readonly CompiledDictionary _dictionary;

	public FuzzyMatcher(CompiledDictionary dictionary)
	{
		_dictionary = dictionary;
	}

	public IReadOnlyList<CorrectionCandidate> Suggest(string targetWord, float maxDistance = 2.0f)
	{
		if (string.IsNullOrWhiteSpace(targetWord) || _fstData.Length == 0 || targetWord.Length > MaxWordLength)
		{
			return Array.Empty<CorrectionCandidate>();
		}

		var topK = new TopCandidates(MaxResults, maxDistance);

		int rowLength = targetWord.Length + 1;
		int matrixSize = MaxWordLength * rowLength;

		Span<float> matrixBuffer = stackalloc float[matrixSize];
		Span<float> initialRow = matrixBuffer.Slice(0, rowLength);
		var initialState = DamerauLevenshteinState.CreateInitial(targetWord, initialRow);
		Span<char> currentWord = stackalloc char[MaxWordLength];

		TraverseFst(0, targetWord, initialState, matrixBuffer, rowLength, currentWord, 0, topK);

		var finalResults = new CorrectionCandidate[topK.Count];
		Array.Copy(topK.Items, finalResults, topK.Count);
		return finalResults;
	}

	private void TraverseFst(
		uint currentOffset, ReadOnlySpan<char> targetWord,
		DamerauLevenshteinState currentState, Span<float> matrixBuffer, int rowLength,
		Span<char> currentWord, int depth, TopCandidates topK)
	{
		float currentBound = topK.BoundingDistance;
		if (!currentState.CanMatch(currentBound)) return;

		byte flags = _fstData[(int)currentOffset];
		byte arcCount = _fstData[(int)currentOffset + 1];

		bool isFinal = (flags & 0x01) != 0;
		bool hasPayload = (flags & 0x02) != 0;
		int ptr = (int)currentOffset + 2;

		if (isFinal && hasPayload)
		{
			float finalDistance = currentState.GetFinalDistance();
			int lengthDifference = Math.Abs(targetWord.Length - depth);

			if (finalDistance <= currentBound && lengthDifference <= currentBound)
			{
				ExtractPayloadAndTryAdd(ptr, currentWord.Slice(0, depth), finalDistance, topK);
			}

			ptr += 1;
			ushort ruleCount = BinaryPrimitives.ReadUInt16LittleEndian(_fstData.AsSpan(ptr));
			ptr += 2 + (ruleCount * 2);
		}

		if (depth + 1 >= MaxWordLength) return;

		int nextDepth = depth + 1;
		Span<float> nextRow = matrixBuffer.Slice(nextDepth * rowLength, rowLength);

		for (int i = 0; i < arcCount; i++)
		{
			char transitionChar = (char)BinaryPrimitives.ReadUInt16LittleEndian(_fstData.AsSpan(ptr));
			uint nextOffset = BinaryPrimitives.ReadUInt32LittleEndian(_fstData.AsSpan(ptr + 2));
			ptr += 6;

			var nextState = currentState.Step(transitionChar, targetWord, nextRow, nextDepth);
			currentWord[depth] = transitionChar;

			TraverseFst(nextOffset, targetWord, nextState, matrixBuffer, rowLength, currentWord, nextDepth, topK);
		}
	}

	private void ExtractPayloadAndTryAdd(int payloadPtr, ReadOnlySpan<char> form, float distance, TopCandidates topK)
	{
		byte frequency = _fstData[payloadPtr];
		payloadPtr += 1;

		ushort ruleCount = BinaryPrimitives.ReadUInt16LittleEndian(_fstData.AsSpan(payloadPtr));
		payloadPtr += 2;

		if (ruleCount > 0)
		{
			var tagsets = new MorphologyTagset[ruleCount];
			for (int i = 0; i < ruleCount; i++)
			{
				ushort ruleId = BinaryPrimitives.ReadUInt16LittleEndian(_fstData.AsSpan(payloadPtr + (i * 2)));
				tagsets[i] = _tagsets[_rules[ruleId].TagId];
			}

			topK.TryAdd(distance, frequency, form, tagsets);
		}
	}
}