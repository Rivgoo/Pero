namespace Pero.Languages.Uk_UA.Dictionaries.Fuzzy;

/// <summary>
/// Represents a state in the Levenshtein Automaton.
/// It tracks the current edit distance for each position in the target word.
/// Designed as a struct to avoid heap allocations during FST traversal.
/// </summary>
public readonly ref struct LevenshteinState
{
	private readonly ReadOnlySpan<int> _distances;

	public LevenshteinState(ReadOnlySpan<int> distances)
	{
		_distances = distances;
	}

	/// <summary>
	/// Creates the initial state for a target word.
	/// </summary>
	public static LevenshteinState CreateInitial(int wordLength, Span<int> buffer)
	{
		for (int i = 0; i <= wordLength; i++)
		{
			buffer[i] = i;
		}
		return new LevenshteinState(buffer);
	}

	/// <summary>
	/// Computes the next state given a character transition in the FST.
	/// </summary>
	public LevenshteinState Step(char c, ReadOnlySpan<char> targetWord, Span<int> nextBuffer)
	{
		nextBuffer[0] = _distances[0] + 1;

		for (int i = 1; i <= targetWord.Length; i++)
		{
			int insertCost = nextBuffer[i - 1] + 1;
			int deleteCost = _distances[i] + 1;

			// Substitute cost is 0 if characters match, 1 otherwise
			int substituteCost = _distances[i - 1] + (targetWord[i - 1] == c ? 0 : 1);

			nextBuffer[i] = Math.Min(insertCost, Math.Min(deleteCost, substituteCost));
		}

		return new LevenshteinState(nextBuffer);
	}

	/// <summary>
	/// Checks if any distance in the current state is within the maximum allowed threshold.
	/// If false, the FST traversal for this branch can be pruned.
	/// </summary>
	public bool CanMatch(int maxDistance)
	{
		foreach (int distance in _distances)
		{
			if (distance <= maxDistance)
				return true;
		}
		return false;
	}

	/// <summary>
	/// Gets the final edit distance for the complete target word.
	/// </summary>
	public int GetFinalDistance() => _distances[_distances.Length - 1];
}