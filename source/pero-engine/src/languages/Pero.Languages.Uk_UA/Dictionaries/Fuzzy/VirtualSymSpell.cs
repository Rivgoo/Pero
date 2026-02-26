using System.Buffers.Binary;
using Pero.Abstractions.Models.Morphology;
using Pero.Languages.Uk_UA.Dictionaries.Models;

namespace Pero.Languages.Uk_UA.Dictionaries.Fuzzy;

public class VirtualSymSpell
{
	private const int MaxWordLength = 24;
	private const float FrequencyBonusWeight = 0.021f;

	private readonly CompiledDictionary _dictionary;
	private readonly byte[] _fstData;
	private readonly FlatMorphologyRule[] _rules;
	private readonly MorphologyTagset[] _tagsets;

	public VirtualSymSpell(CompiledDictionary dictionary)
	{
		_dictionary = dictionary;
		_fstData = dictionary.FstData;
		_rules = dictionary.Rules;
		_tagsets = dictionary.Tagsets;
	}

	public IEnumerable<CorrectionCandidate> GetCandidates(string word)
	{
		if (string.IsNullOrWhiteSpace(word) || word.Length > MaxWordLength || _fstData.Length == 0)
		{
			return Array.Empty<CorrectionCandidate>();
		}

		var candidates = new List<CorrectionCandidate>();
		Span<char> buffer = stackalloc char[word.Length + 1];

		for (int i = 0; i < word.Length; i++)
		{
			word.AsSpan(0, i).CopyTo(buffer);
			word.AsSpan(i + 1).CopyTo(buffer.Slice(i));

			float cost = PenaltyMatrix.GetDeletionCost(word[i]) * PenaltyMatrix.GetPositionalMultiplier(i, word.Length);

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

			float cost = 0.4f * PenaltyMatrix.GetPositionalMultiplier(i, word.Length);
			CheckAndAdd(buffer.Slice(0, word.Length), cost, candidates);

			(buffer[i], buffer[i + 1]) = (buffer[i + 1], buffer[i]);
		}

		for (int i = 0; i < word.Length; i++)
		{
			char originalChar = word[i];
			foreach (char c in PenaltyMatrix.UkrainianAlphabet)
			{
				if (c == originalChar) continue;

				float cost = PenaltyMatrix.GetSubstitutionCostUnsafe(originalChar, c) * PenaltyMatrix.GetPositionalMultiplier(i, word.Length);

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

			foreach (char c in PenaltyMatrix.UkrainianAlphabet)
			{
				buffer[i] = c;
				float cost = PenaltyMatrix.GetInsertionCost(c) * PenaltyMatrix.GetPositionalMultiplier(i, word.Length + 1);

				if (c == 'ь' || c == '\'') cost -= 0.5f;

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
				var tagset = _tagsets[_rules[ruleIds[i]].TagId];
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
			byte flags = _fstData[(int)currentOffset];
			byte arcCount = _fstData[(int)currentOffset + 1];
			int ptr = (int)currentOffset + 2;

			if ((flags & 0x02) != 0)
			{
				ptr += 1;
				ushort ruleCount = BinaryPrimitives.ReadUInt16LittleEndian(_fstData.AsSpan(ptr));
				ptr += 2 + (ruleCount * 2);
			}

			bool found = false;
			for (int i = 0; i < arcCount; i++)
			{
				char transitionChar = (char)BinaryPrimitives.ReadUInt16LittleEndian(_fstData.AsSpan(ptr));
				if (transitionChar == c)
				{
					currentOffset = BinaryPrimitives.ReadUInt32LittleEndian(_fstData.AsSpan(ptr + 2));
					found = true;
					break;
				}
				ptr += 6;
			}

			if (!found) return false;
		}

		byte finalFlags = _fstData[(int)currentOffset];

		if ((finalFlags & 0x01) == 0 || (finalFlags & 0x02) == 0) return false;

		int payloadPtr = (int)currentOffset + 2;

		frequency = _fstData[payloadPtr];
		payloadPtr += 1;

		ushort finalRuleCount = BinaryPrimitives.ReadUInt16LittleEndian(_fstData.AsSpan(payloadPtr));
		payloadPtr += 2;

		if (finalRuleCount > 0)
		{
			ruleIds = new ushort[finalRuleCount];
			for (int i = 0; i < finalRuleCount; i++)
			{
				ruleIds[i] = BinaryPrimitives.ReadUInt16LittleEndian(_fstData.AsSpan(payloadPtr));
				payloadPtr += 2;
			}
			return true;
		}

		return false;
	}
}