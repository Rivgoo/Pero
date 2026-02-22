using System.Runtime.CompilerServices;

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

	public static DamerauLevenshteinState CreateInitial(ReadOnlySpan<char> targetWord, Span<float> buffer, MatcherContext context)
	{
		buffer[0] = 0f;
		for (int i = 1; i <= targetWord.Length; i++)
		{
			float insertCost = (i > 1 && targetWord[i - 1] == targetWord[i - 2])
				? 0.1f
				: context.InsertionCosts[i - 1];

			buffer[i] = buffer[i - 1] + (insertCost * context.PositionMultipliers[i - 1]);
		}
		return new DamerauLevenshteinState(buffer, ReadOnlySpan<float>.Empty, '\0');
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public DamerauLevenshteinState Step(
		char candidateChar,
		ReadOnlySpan<char> userWord,
		Span<float> currentBuffer,
		int currentDepth,
		MatcherContext context)
	{
		currentBuffer[0] = _prevDistances[0] + PenaltyMatrix.GetDeletionCost(candidateChar);
		int userLen = userWord.Length;

		for (int i = 1; i <= userLen; i++)
		{
			char userChar = userWord[i - 1];
			float multiplier = context.PositionMultipliers[i - 1];

			float insertCost = (i > 1 && userChar == userWord[i - 2])
				? currentBuffer[i - 1] + (0.1f * multiplier)
				: currentBuffer[i - 1] + (context.InsertionCosts[i - 1] * multiplier);

			float deleteCost = (candidateChar == _prevChar)
				? _prevDistances[i] + (0.1f * multiplier)
				: _prevDistances[i] + (PenaltyMatrix.GetDeletionCost(candidateChar) * multiplier);

			float subCost = _prevDistances[i - 1] + (PenaltyMatrix.GetSubstitutionCostUnsafe(userChar, candidateChar) * multiplier);

			float minCost = insertCost < deleteCost ? insertCost : deleteCost;
			if (subCost < minCost) minCost = subCost;

			if (i > 1 && !_prevPrevDistances.IsEmpty)
			{
				if (userWord[i - 1] == _prevChar && userWord[i - 2] == candidateChar)
				{
					float transpositionCost = _prevPrevDistances[i - 2] + (0.4f * multiplier);
					if (transpositionCost < minCost) minCost = transpositionCost;
				}
			}

			currentBuffer[i] = minCost;
		}

		return new DamerauLevenshteinState(currentBuffer, _prevDistances, candidateChar);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool CanMatch(float maxDistance)
	{
		var span = _prevDistances;

		for (int i = 0; i < span.Length; i++)
		{
			if (span[i] <= maxDistance) return true;
		}
		return false;
	}

	public float GetFinalDistance() => _prevDistances[_prevDistances.Length - 1];
}