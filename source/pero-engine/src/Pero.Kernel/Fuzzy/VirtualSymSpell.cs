using System.Buffers.Binary;
using Pero.Abstractions.Models.Morphology;
using Pero.Kernel.Dictionaries;

namespace Pero.Kernel.Fuzzy;

public class VirtualSymSpell
{
	private const int MaxWordLength = 24;
	private const float FrequencyBonusWeight = 0.021f;

	private readonly CompiledDictionary _dictionary;
	private readonly BasePenaltyMatrix _penaltyMatrix;

	public VirtualSymSpell(CompiledDictionary dictionary, BasePenaltyMatrix penaltyMatrix)
	{
		_dictionary = dictionary;
		_penaltyMatrix = penaltyMatrix;
	}

	public IEnumerable<CorrectionCandidate> GetCandidates(string word)
	{
		if (string.IsNullOrWhiteSpace(word) || word.Length > MaxWordLength || _dictionary.FstData.Length == 0)
		{
			return Array.Empty<CorrectionCandidate>();
		}

		var candidates = new List<CorrectionCandidate>();
		Span<char> buffer = stackalloc char[word.Length + 1];

		for (int i = 0; i < word.Length; i++)
		{
			word.AsSpan(0, i).CopyTo(buffer);
			word.AsSpan(i + 1).CopyTo(buffer.Slice(i));

			float cost = _penaltyMatrix.GetDeletionCost(word[i]) * _penaltyMatrix.GetPositionalMultiplier(i, word.Length);

			if ((i > 0 && word[i] == word[i - 1]) || (i < word.Length - 1 && word[i] == word[i + 1]))
			{
				cost -= 2.0f;
			}

			CheckAndAdd(buffer.Slice(0, word.Length - 1), cost, candidates);
		}

		word.AsSpan().CopyTo(buffer);
		for (int i = 0; i < word.Length - 1; i++)
		{
			(buffer[i], buffer[i + 1]) = (buffer[i + 1], buffer[i]);

			float cost = 0.4f * _penaltyMatrix.GetPositionalMultiplier(i, word.Length);
			CheckAndAdd(buffer.Slice(0, word.Length), cost, candidates);

			(buffer[i], buffer[i + 1]) = (buffer[i + 1], buffer[i]);
		}

		for (int i = 0; i < word.Length; i++)
		{
			char originalChar = word[i];
			foreach (char c in _penaltyMatrix.Alphabet)
			{
				if (c == originalChar) continue;

				float cost = _penaltyMatrix.GetSubstitutionCostUnsafe(originalChar, c) * _penaltyMatrix.GetPositionalMultiplier(i, word.Length);

				if (cost < 0.8f)
				{
					buffer[i] = c;
					CheckAndAdd(buffer.Slice(0, word.Length), cost, candidates);
				}
			}
			buffer[i] = originalChar;
		}

		for (int i = 0; i <= word.Length; i++)
		{
			word.AsSpan(0, i).CopyTo(buffer);
			word.AsSpan(i).CopyTo(buffer.Slice(i + 1));

			foreach (char c in _penaltyMatrix.Alphabet)
			{
				buffer[i] = c;
				float cost = _penaltyMatrix.GetInsertionCost(c) * _penaltyMatrix.GetPositionalMultiplier(i, word.Length + 1);

				CheckAndAdd(buffer.Slice(0, word.Length + 1), cost, candidates);
			}
		}

		return candidates;
	}

	public IEnumerable<CorrectionCandidate> GetSplitCandidates(string word)
	{
		if (word.Length < 4) return Array.Empty<CorrectionCandidate>();

		var candidates = new List<CorrectionCandidate>();

		for (int i = 1; i < word.Length; i++)
		{
			var leftSpan = word.AsSpan(0, i);
			var rightSpan = word.AsSpan(i);

			if (FastCheckFst(leftSpan, out byte leftFreq, out _) &&
				FastCheckFst(rightSpan, out byte rightFreq, out _))
			{
				float cost = (leftSpan.Length == 1 || rightSpan.Length == 1) ? 0.3f : 0.6f;
				byte combinedFreq = Math.Min(leftFreq, rightFreq);
				string suggestedText = $"{leftSpan.ToString()} {rightSpan.ToString()}";

				candidates.Add(new CorrectionCandidate(suggestedText, cost, combinedFreq, cost, Array.Empty<MorphologyTagset>()));
			}
		}

		return candidates;
	}

	private void CheckAndAdd(ReadOnlySpan<char> candidateSpan, float cost, List<CorrectionCandidate> candidates)
	{
		foreach (var existing in candidates)
		{
			if (candidateSpan.SequenceEqual(existing.Word.AsSpan())) return;
		}

		if (FastCheckFst(candidateSpan, out byte frequency, out ushort[] ruleIds))
		{
			var distinctTagsets = new List<MorphologyTagset>(ruleIds.Length);

			for (int i = 0; i < ruleIds.Length; i++)
			{
				var tagset = _dictionary.Tagsets[_dictionary.Rules[ruleIds[i]].TagId];
				if (!distinctTagsets.Contains(tagset))
				{
					distinctTagsets.Add(tagset);
				}
			}

			float score = cost - (frequency * FrequencyBonusWeight);
			candidates.Add(new CorrectionCandidate(candidateSpan.ToString(), cost, frequency, score, distinctTagsets.ToArray()));
		}
	}

	private bool FastCheckFst(ReadOnlySpan<char> word, out byte frequency, out ushort[] ruleIds)
	{
		frequency = 0;
		ruleIds = Array.Empty<ushort>();
		uint currentOffset = 0;

		foreach (var c in word)
		{
			byte flags = _dictionary.FstData[(int)currentOffset];
			byte arcCount = _dictionary.FstData[(int)currentOffset + 1];
			int ptr = (int)currentOffset + 2;

			if ((flags & 0x02) != 0)
			{
				ptr += 1;
				ushort ruleCount = BinaryPrimitives.ReadUInt16LittleEndian(_dictionary.FstData.AsSpan(ptr));
				ptr += 2 + (ruleCount * 2);
			}

			bool found = false;
			for (int i = 0; i < arcCount; i++)
			{
				char transitionChar = (char)BinaryPrimitives.ReadUInt16LittleEndian(_dictionary.FstData.AsSpan(ptr));
				if (transitionChar == c)
				{
					currentOffset = BinaryPrimitives.ReadUInt32LittleEndian(_dictionary.FstData.AsSpan(ptr + 2));
					found = true;
					break;
				}
				ptr += 6;
			}

			if (!found) return false;
		}

		byte finalFlags = _dictionary.FstData[(int)currentOffset];

		if ((finalFlags & 0x01) == 0 || (finalFlags & 0x02) == 0) return false;

		int payloadPtr = (int)currentOffset + 2;

		frequency = _dictionary.FstData[payloadPtr];
		payloadPtr += 1;

		ushort finalRuleCount = BinaryPrimitives.ReadUInt16LittleEndian(_dictionary.FstData.AsSpan(payloadPtr));
		payloadPtr += 2;

		if (finalRuleCount > 0)
		{
			ruleIds = new ushort[finalRuleCount];
			for (int i = 0; i < finalRuleCount; i++)
			{
				ruleIds[i] = BinaryPrimitives.ReadUInt16LittleEndian(_dictionary.FstData.AsSpan(payloadPtr));
				payloadPtr += 2;
			}
			return true;
		}

		return false;
	}
}