using Pero.Abstractions.Models.Morphology;
using Pero.Kernel.Dictionaries;

namespace Pero.Kernel.Fuzzy;

public class VirtualSymSpell<TTag> where TTag : MorphologicalTag
{
	private const int MaxWordLength = 24;
	private const float FrequencyBonusWeight = 0.021f;

	private readonly FstSuffixDictionary<TTag> _dictionary;
	private readonly BasePenaltyMatrix _penaltyMatrix;

	public VirtualSymSpell(FstSuffixDictionary<TTag> dictionary, BasePenaltyMatrix penaltyMatrix)
	{
		_dictionary = dictionary;
		_penaltyMatrix = penaltyMatrix;
	}

	public IEnumerable<CorrectionCandidate<TTag>> GetCandidates(string word)
	{
		if (string.IsNullOrWhiteSpace(word) || word.Length > MaxWordLength || _dictionary.FstData.Length == 0)
		{
			return Array.Empty<CorrectionCandidate<TTag>>();
		}

		var candidates = new List<CorrectionCandidate<TTag>>();
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

				if (cost <= 1.1f)
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

	public IEnumerable<CorrectionCandidate<TTag>> GetSplitCandidates(string word)
	{
		if (word.Length < 4) return Array.Empty<CorrectionCandidate<TTag>>();

		var candidates = new List<CorrectionCandidate<TTag>>();

		for (int i = 1; i < word.Length; i++)
		{
			var leftSpan = word.AsSpan(0, i);
			var rightSpan = word.AsSpan(i);

			if (_dictionary.TryGetFrequencyAndTags(leftSpan, out byte leftFreq, out _) &&
				_dictionary.TryGetFrequencyAndTags(rightSpan, out byte rightFreq, out _))
			{
				float cost = (leftSpan.Length == 1 || rightSpan.Length == 1) ? 0.3f : 0.6f;
				byte combinedFreq = Math.Min(leftFreq, rightFreq);
				string suggestedText = $"{leftSpan.ToString()} {rightSpan.ToString()}";

				candidates.Add(new CorrectionCandidate<TTag>(suggestedText, cost, combinedFreq, cost, Array.Empty<TTag>()));
			}
		}

		return candidates;
	}

	private void CheckAndAdd(ReadOnlySpan<char> candidateSpan, float cost, List<CorrectionCandidate<TTag>> candidates)
	{
		foreach (var existing in candidates)
		{
			if (candidateSpan.SequenceEqual(existing.Word.AsSpan())) return;
		}

		if (_dictionary.TryGetFrequencyAndTags(candidateSpan, out byte frequency, out TTag[] tags))
		{
			float score = cost - (frequency * FrequencyBonusWeight);
			candidates.Add(new CorrectionCandidate<TTag>(candidateSpan.ToString(), cost, frequency, score, tags));
		}
	}
}