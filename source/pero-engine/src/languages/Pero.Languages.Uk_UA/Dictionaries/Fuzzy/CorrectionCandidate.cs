namespace Pero.Languages.Uk_UA.Dictionaries.Fuzzy;

/// <summary>
/// Represents a potential correction for a misspelled word.
/// </summary>
public readonly struct CorrectionCandidate : IComparable<CorrectionCandidate>
{
	public string Word { get; }
	public int Distance { get; }
	public byte Frequency { get; }

	public CorrectionCandidate(string word, int distance, byte frequency)
	{
		Word = word;
		Distance = distance;
		Frequency = frequency;
	}

	/// <summary>
	/// Sorts candidates: first by edit distance (closer matches are better),
	/// then by frequency (more common words are better).
	/// </summary>
	public int CompareTo(CorrectionCandidate other)
	{
		// 1. Smaller distance is better
		int distanceComparison = Distance.CompareTo(other.Distance);
		if (distanceComparison != 0) return distanceComparison;

		// 2. Higher frequency is better (hence the reverse comparison)
		return other.Frequency.CompareTo(Frequency);
	}

	public override string ToString() => $"{Word} (D:{Distance}, F:{Frequency})";
}