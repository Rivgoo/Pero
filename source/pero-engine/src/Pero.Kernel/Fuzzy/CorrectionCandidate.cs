using Pero.Abstractions.Models.Morphology;

namespace Pero.Kernel.Fuzzy;

public readonly struct CorrectionCandidate : IComparable<CorrectionCandidate>
{
	public string Word { get; }
	public float Distance { get; }
	public byte Frequency { get; }
	public float Score { get; }
	public MorphologyTagset[] Tagsets { get; }

	public CorrectionCandidate(string word, float distance, byte frequency, float score, MorphologyTagset[] tagsets)
	{
		Word = word;
		Distance = distance;
		Frequency = frequency;
		Score = score;
		Tagsets = tagsets;
	}

	public int CompareTo(CorrectionCandidate other)
	{
		int scoreComparison = Score.CompareTo(other.Score);
		if (scoreComparison != 0) return scoreComparison;
		return other.Frequency.CompareTo(Frequency);
	}

	public override string ToString() => $"{Word} (S:{Score:F2} D:{Distance:F2} F:{Frequency})";
}