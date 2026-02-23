namespace Pero.Languages.Uk_UA.Dictionaries.Fuzzy;

public class VirtualSymSpell
{
	private readonly CompiledDictionary _dictionary;
	private const float FrequencyBonusWeight = 0.02f;

	public VirtualSymSpell(CompiledDictionary dictionary)
	{
		_dictionary = dictionary;
	}

	public IEnumerable<CorrectionCandidate> GetCandidates(string word)
	{
		var seen = new HashSet<string>(StringComparer.Ordinal);
		var candidates = new List<CorrectionCandidate>();

		void CheckAndAdd(string candidateWord, float baseDistance)
		{
			if (!seen.Add(candidateWord)) return;

			var results = _dictionary.Analyze(candidateWord).ToList();
			if (results.Count > 0)
			{
				byte freq = ExtractFrequency(candidateWord);
				float score = baseDistance - (freq * FrequencyBonusWeight);

				var tagsets = results.Select(r => r.Tagset).Distinct().ToArray();
				candidates.Add(new CorrectionCandidate(candidateWord, baseDistance, freq, score, tagsets));
			}
		}

		for (int i = 0; i < word.Length; i++)
		{
			string candidate = word.Remove(i, 1);
			float cost = PenaltyMatrix.GetDeletionCost(word[i]) * PenaltyMatrix.GetPositionalMultiplier(i, word.Length);

			if ((i > 0 && word[i] == word[i - 1]) || (i < word.Length - 1 && word[i] == word[i + 1]))
			{
				cost -= 2.0f;
			}

			CheckAndAdd(candidate, cost);
		}

		for (int i = 0; i < word.Length - 1; i++)
		{
			var chars = word.ToCharArray();
			(chars[i], chars[i + 1]) = (chars[i + 1], chars[i]);
			float cost = 0.4f * PenaltyMatrix.GetPositionalMultiplier(i, word.Length);
			CheckAndAdd(new string(chars), cost);
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
					var chars = word.ToCharArray();
					chars[i] = c;
					CheckAndAdd(new string(chars), cost);
				}
			}
		}

		for (int i = 0; i <= word.Length; i++)
		{
			foreach (char c in PenaltyMatrix.UkrainianAlphabet)
			{
				string candidate = word.Insert(i, c.ToString());
				float cost = PenaltyMatrix.GetInsertionCost(c) * PenaltyMatrix.GetPositionalMultiplier(i, word.Length + 1);

				if (c == 'ь' || c == '\'') cost -= 0.5f;

				CheckAndAdd(candidate, cost);
			}
		}

		return candidates;
	}

	private byte ExtractFrequency(string word)
	{
		var data = _dictionary.FstData;
		uint currentOffset = 0;

		foreach (var c in word)
		{
			byte arcCount = data[(int)currentOffset + 1];
			int ptr = (int)currentOffset + 2;

			bool hasPayload = (data[(int)currentOffset] & 0x02) != 0;
			if (hasPayload)
			{
				ptr += 1;
				ushort ruleCount = System.Buffers.Binary.BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(ptr));
				ptr += 2 + (ruleCount * 2);
			}

			for (int i = 0; i < arcCount; i++)
			{
				char transitionChar = (char)System.Buffers.Binary.BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(ptr));
				if (transitionChar == c)
				{
					currentOffset = System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(ptr + 2));
					break;
				}
				ptr += 6;
			}
		}

		if ((data[(int)currentOffset] & 0x02) != 0)
		{
			return data[(int)currentOffset + 2];
		}
		return 0;
	}
}