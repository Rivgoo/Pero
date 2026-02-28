using Pero.Abstractions.Models.Morphology;

namespace Pero.Kernel.Fuzzy;

public readonly struct CorrectionCandidate<TTag> : IComparable<CorrectionCandidate<TTag>> where TTag : MorphologicalTag
{
	public string Word { get; }
	public float Distance { get; }
	public byte Frequency { get; }
	public float Score { get; }
	public TTag[] Tagsets { get; }

	public CorrectionCandidate(string word, float distance, byte frequency, float score, TTag[] tagsets)
	{
		Word = word;
		Distance = distance;
		Frequency = frequency;
		Score = score;
		Tagsets = tagsets;
	}

	public int CompareTo(CorrectionCandidate<TTag> other)
	{
		int scoreComparison = Score.CompareTo(other.Score);
		if (scoreComparison != 0) return scoreComparison;
		return other.Frequency.CompareTo(Frequency);
	}

	public override string ToString() => $"{Word} (S:{Score:F2} D:{Distance:F2} F:{Frequency})";
}