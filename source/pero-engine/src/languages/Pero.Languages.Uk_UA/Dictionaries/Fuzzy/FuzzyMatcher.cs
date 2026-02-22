using Pero.Abstractions.Models.Morphology;
using Pero.Languages.Uk_UA.Dictionaries.Models;
using System.Buffers.Binary;
using System.Collections.Concurrent;

namespace Pero.Languages.Uk_UA.Dictionaries.Fuzzy;

public class FuzzyMatcher
{
	private const int TargetResults = 8;
	private const int MaxWordLength = 24;
	private const float FrequencyBonusWeight = 0.02f;
	private const float MaxFrequencyBonus = 31 * FrequencyBonusWeight;

	private byte[] _fstData => _dictionary.FstData;
	private FlatMorphologyRule[] _rules => _dictionary.Rules;
	private MorphologyTagset[] _tagsets => _dictionary.Tagsets;

	private readonly CompiledDictionary _dictionary;
	private readonly ConcurrentDictionary<string, CorrectionCandidate[]> _cache = new(StringComparer.Ordinal);
	private readonly MatcherContext _context = new();

	public FuzzyMatcher(CompiledDictionary dictionary)
	{
		_dictionary = dictionary;
	}

	public CorrectionCandidate[] Suggest(string targetWord)
	{
		if (string.IsNullOrWhiteSpace(targetWord) || _fstData.Length == 0 || targetWord.Length > MaxWordLength)
			return Array.Empty<CorrectionCandidate>();

		if (_cache.TryGetValue(targetWord, out var cached)) return cached;

		_context.InitializeForWord(targetWord);

		float initialMaxDist = targetWord.Length <= 4 ? 3f : Math.Min(targetWord.Length - 2f, 4f);
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
		// 1. Dynamic pruning using the best candidates found so far
		float dynamicBound = pool.WorstScore + MaxFrequencyBonus;
		if (!currentState.CanMatch(dynamicBound)) return;

		byte flags = _fstData[(int)currentOffset];
		byte arcCount = _fstData[(int)currentOffset + 1];
		byte minDistToFinal = _fstData[(int)currentOffset + 2];
		byte maxDistToFinal = _fstData[(int)currentOffset + 3];

		// 2. Structural/Geometric pruning based on word length
		int minPossibleLength = depth + minDistToFinal;
		int maxPossibleLength = depth + maxDistToFinal;
		int targetLen = targetWord.Length;

		// Relax length constraints slightly to account for insertions/deletions cost variations
		if (minPossibleLength > targetLen + (dynamicBound * 1.5f)) return;
		if (maxPossibleLength < targetLen - (dynamicBound * 1.5f)) return;

		int ptr = (int)currentOffset + 4;
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
			ptr += 1; // Freq
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

			var nextState = currentState.Step(transitionChar, targetWord, nextRow, nextDepth, _context);
			currentWord[depth] = transitionChar;

			TraverseFst(nextOffset, targetWord, nextState, matrixBuffer, rowLength, currentWord, nextDepth, pool);

			// Re-check dynamic bound after returning from recursion
			if (pool.IsFull && !currentState.CanMatch(pool.WorstScore + MaxFrequencyBonus)) return;
		}
	}

	private void ExtractPayloadAndTryAdd(int payloadPtr, ReadOnlySpan<char> form, float distance, CandidatePool pool)
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
			// Calculate Score: Distance reduced by Frequency
			float score = distance - (frequency * FrequencyBonusWeight);
			pool.TryAdd(new CorrectionCandidate(form.ToString(), distance, frequency, score, tagsets));
		}
	}

	private class CandidatePool
	{
		private readonly CorrectionCandidate[] _items;
		private int _count;
		private readonly int _targetCapacity;
		public float WorstScore { get; private set; }
		public bool IsFull => _count >= _targetCapacity;

		public CandidatePool(int targetCapacity, float initialMaxDistance)
		{
			_targetCapacity = targetCapacity;
			_items = new CorrectionCandidate[targetCapacity + 1];
			WorstScore = initialMaxDistance;
		}

		public void TryAdd(CorrectionCandidate candidate)
		{
			if (IsFull && candidate.Score >= WorstScore) return;

			// De-duplication: keep better score
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
			// Tighten the bound to the worst acceptable candidate
			if (IsFull) WorstScore = _items[_count - 1].Score;
		}

		public CorrectionCandidate[] GetFinalResults()
		{
			var result = new CorrectionCandidate[_count];
			Array.Copy(_items, result, _count);
			return result;
		}
	}
}