using Pero.Abstractions.Models.Morphology;
using Pero.Kernel.Dictionaries;
using System.Buffers.Binary;
using System.Collections.Concurrent;

namespace Pero.Kernel.Fuzzy;

public class FuzzyMatcher<TTag> where TTag : MorphologicalTag
{
	private const int TargetResults = 1;
	private const int MaxWordLength = 32;
	private const float FrequencyBonusWeight = 0.05f;
	private const float MaxFrequencyBonus = 31 * FrequencyBonusWeight;

	private readonly FstSuffixDictionary<TTag> _dictionary;
	private readonly ConcurrentDictionary<string, CorrectionCandidate<TTag>[]> _cache = new(StringComparer.Ordinal);
	private readonly MatcherContext _context;

	public FuzzyMatcher(FstSuffixDictionary<TTag> dictionary, BasePenaltyMatrix penaltyMatrix)
	{
		_dictionary = dictionary;
		_context = new MatcherContext(penaltyMatrix);
	}

	public CorrectionCandidate<TTag>[] Suggest(string targetWord)
	{
		if (string.IsNullOrWhiteSpace(targetWord) || _dictionary.FstData.Length == 0 || targetWord.Length > MaxWordLength)
			return Array.Empty<CorrectionCandidate<TTag>>();

		if (_cache.TryGetValue(targetWord, out var cached)) return cached;

		_context.InitializeForWord(targetWord);

		float initialMaxDist = 2;
		var pool = new CandidatePool(TargetResults, initialMaxDist);

		int rowLength = targetWord.Length + 1;
		int matrixSize = MaxWordLength * rowLength;
		Span<float> matrixBuffer = stackalloc float[matrixSize];
		Span<float> initialRow = matrixBuffer.Slice(0, rowLength);

		var initialState = DamerauLevenshteinState.CreateInitial(targetWord, initialRow, _context);
		Span<char> currentWord = stackalloc char[MaxWordLength];

		TraverseFst(0, targetWord, initialState, matrixBuffer, rowLength, currentWord, 0, pool);

		var finalResults = pool.GetFinalResults();
		_cache[targetWord] = finalResults;
		return finalResults;
	}

	private void TraverseFst(
		uint currentOffset, ReadOnlySpan<char> targetWord,
		DamerauLevenshteinState currentState, Span<float> matrixBuffer, int rowLength,
		Span<char> currentWord, int depth, CandidatePool pool)
	{
		float dynamicBound = pool.WorstScore + MaxFrequencyBonus;

		if (!currentState.CanMatch(dynamicBound)) return;

		byte flags = _dictionary.FstData[(int)currentOffset];
		byte arcCount = _dictionary.FstData[(int)currentOffset + 1];

		int ptr = (int)currentOffset + 2;
		bool isFinal = (flags & 0x01) != 0;
		bool hasPayload = (flags & 0x02) != 0;

		if (isFinal && hasPayload)
		{
			float finalDistance = currentState.GetFinalDistance();
			if (finalDistance <= dynamicBound)
			{
				ExtractPayloadAndTryAdd(ptr, currentWord.Slice(0, depth), finalDistance, pool);
				dynamicBound = pool.WorstScore + MaxFrequencyBonus;
			}
			ptr += 1;
			ushort ruleCount = BinaryPrimitives.ReadUInt16LittleEndian(_dictionary.FstData.AsSpan(ptr));
			ptr += 2 + (ruleCount * 2);
		}

		if (depth + 1 >= MaxWordLength) return;

		int nextDepth = depth + 1;
		Span<float> nextRow = matrixBuffer.Slice(nextDepth * rowLength, rowLength);

		for (int i = 0; i < arcCount; i++)
		{
			char transitionChar = (char)BinaryPrimitives.ReadUInt16LittleEndian(_dictionary.FstData.AsSpan(ptr));
			uint nextOffset = BinaryPrimitives.ReadUInt32LittleEndian(_dictionary.FstData.AsSpan(ptr + 2));
			ptr += 6;

			var nextState = currentState.Step(transitionChar, targetWord, nextRow, nextDepth, _context);
			currentWord[depth] = transitionChar;

			TraverseFst(nextOffset, targetWord, nextState, matrixBuffer, rowLength, currentWord, nextDepth, pool);

			if (pool.IsFull && !currentState.CanMatch(pool.WorstScore + MaxFrequencyBonus)) return;
		}
	}

	private void ExtractPayloadAndTryAdd(int payloadPtr, ReadOnlySpan<char> form, float distance, CandidatePool pool)
	{
		byte frequency = _dictionary.FstData[payloadPtr];
		payloadPtr += 1;
		ushort ruleCount = BinaryPrimitives.ReadUInt16LittleEndian(_dictionary.FstData.AsSpan(payloadPtr));
		payloadPtr += 2;

		if (ruleCount > 0)
		{
			var tagsets = new TTag[ruleCount];
			for (int i = 0; i < ruleCount; i++)
			{
				ushort ruleId = BinaryPrimitives.ReadUInt16LittleEndian(_dictionary.FstData.AsSpan(payloadPtr + (i * 2)));
				tagsets[i] = _dictionary.Tagsets[_dictionary.Rules[ruleId].TagId];
			}
			float score = distance - (frequency * FrequencyBonusWeight);
			pool.TryAdd(new CorrectionCandidate<TTag>(form.ToString(), distance, frequency, score, tagsets));
		}
	}

	private class CandidatePool
	{
		private readonly CorrectionCandidate<TTag>[] _items;
		private int _count;
		private readonly int _targetCapacity;
		public float WorstScore { get; private set; }
		public bool IsFull => _count >= _targetCapacity;

		public CandidatePool(int targetCapacity, float initialMaxDistance)
		{
			_targetCapacity = targetCapacity;
			_items = new CorrectionCandidate<TTag>[targetCapacity];
			WorstScore = initialMaxDistance;
		}

		public void TryAdd(CorrectionCandidate<TTag> candidate)
		{
			if (IsFull && candidate.Score >= WorstScore) return;

			for (int i = 0; i < _count; i++)
			{
				if (_items[i].Word == candidate.Word)
				{
					if (candidate.Score < _items[i].Score)
					{
						_items[i] = candidate;
						Prune();
					}
					return;
				}
			}

			if (_count < _items.Length) _items[_count++] = candidate;
			else _items[_count - 1] = candidate;
			Prune();
		}

		private void Prune()
		{
			Array.Sort(_items, 0, _count);
			if (_count > _targetCapacity) _count = _targetCapacity;
			if (IsFull) WorstScore = _items[_count - 1].Score;
		}

		public CorrectionCandidate<TTag>[] GetFinalResults()
		{
			if (_count == 0) return Array.Empty<CorrectionCandidate<TTag>>();
			var result = new CorrectionCandidate<TTag>[_count];
			Array.Copy(_items, result, _count);
			return result;
		}
	}
}