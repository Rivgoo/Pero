using Pero.Abstractions.Models.Morphology;

namespace Pero.Languages.Uk_UA.Dictionaries.Fuzzy;

public readonly struct CorrectionCandidate : IComparable<CorrectionCandidate>
{
	public string Word { get; }
	public float Distance { get; }
	public byte Frequency { get; }

	public MorphologyTagset[] Tagsets { get; }

	public CorrectionCandidate(string word, float distance, byte frequency, MorphologyTagset[] tagsets)
	{
		Word = word;
		Distance = distance;
		Frequency = frequency;
		Tagsets = tagsets;
	}

	public int CompareTo(CorrectionCandidate other)
	{
		int distanceComparison = Distance.CompareTo(other.Distance);
		if (distanceComparison != 0) return distanceComparison;

		return other.Frequency.CompareTo(Frequency);
	}

	public override string ToString() => $"{Word} (D:{Distance:F2}, F:{Frequency})";
}