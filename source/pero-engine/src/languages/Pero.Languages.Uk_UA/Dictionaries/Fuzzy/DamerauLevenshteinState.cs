namespace Pero.Languages.Uk_UA.Dictionaries.Fuzzy;

public readonly ref struct DamerauLevenshteinState
{
	private readonly ReadOnlySpan<float> _prevDistances;
	private readonly ReadOnlySpan<float> _prevPrevDistances;
	private readonly char _prevChar;

	public DamerauLevenshteinState(
		ReadOnlySpan<float> prevDistances,
		ReadOnlySpan<float> prevPrevDistances,
		char prevChar)
	{
		_prevDistances = prevDistances;
		_prevPrevDistances = prevPrevDistances;
		_prevChar = prevChar;
	}

	public static DamerauLevenshteinState CreateInitial(ReadOnlySpan<char> targetWord, Span<float> buffer)
	{
		buffer[0] = 0f;
		for (int i = 1; i <= targetWord.Length; i++)
		{
			float insertCost;
			if (i > 1 && targetWord[i - 1] == targetWord[i - 2])
			{
				insertCost = 0.1f; // Stutter penalty
			}
			else
			{
				insertCost = PenaltyMatrix.GetInsertionCost(targetWord[i - 1]);
			}

			buffer[i] = buffer[i - 1] + (insertCost * PenaltyMatrix.GetPositionalMultiplier(i - 1, targetWord.Length));
		}

		return new DamerauLevenshteinState(buffer, ReadOnlySpan<float>.Empty, '\0');
	}

	public DamerauLevenshteinState Step(
		char candidateChar,
		ReadOnlySpan<char> userWord,
		Span<float> currentBuffer,
		int currentDepth)
	{
		currentBuffer[0] = _prevDistances[0] + PenaltyMatrix.GetDeletionCost(candidateChar);

		for (int i = 1; i <= userWord.Length; i++)
		{
			char userChar = userWord[i - 1];
			float multiplier = PenaltyMatrix.GetPositionalMultiplier(i - 1, userWord.Length);

			float insertCost;
			if (i > 1 && userWord[i - 1] == userWord[i - 2])
			{
				insertCost = currentBuffer[i - 1] + (0.1f * multiplier);
			}
			else
			{
				insertCost = currentBuffer[i - 1] + (PenaltyMatrix.GetInsertionCost(userChar) * multiplier);
			}

			float deleteCost = _prevDistances[i] + (PenaltyMatrix.GetDeletionCost(candidateChar) * multiplier);
			float subCost = _prevDistances[i - 1] + (PenaltyMatrix.GetSubstitutionCost(userChar, candidateChar) * multiplier);

			float minCost = Math.Min(insertCost, Math.Min(deleteCost, subCost));

			if (i > 1 && !_prevPrevDistances.IsEmpty && _prevChar != '\0')
			{
				if (userWord[i - 1] == _prevChar && userWord[i - 2] == candidateChar)
				{
					float transpositionCost = _prevPrevDistances[i - 2] + (0.6f * multiplier);
					if (transpositionCost < minCost)
					{
						minCost = transpositionCost;
					}
				}
			}

			currentBuffer[i] = minCost;
		}

		return new DamerauLevenshteinState(currentBuffer, _prevDistances, candidateChar);
	}

	public bool CanMatch(float maxDistance)
	{
		foreach (float distance in _prevDistances)
		{
			if (distance <= maxDistance) return true;
		}
		return false;
	}

	public float GetFinalDistance() => _prevDistances[_prevDistances.Length - 1];
}