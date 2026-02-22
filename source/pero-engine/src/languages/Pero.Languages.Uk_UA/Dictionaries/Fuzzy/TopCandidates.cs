using Pero.Abstractions.Models.Morphology;

namespace Pero.Languages.Uk_UA.Dictionaries.Fuzzy;

internal class TopCandidates
{
	public readonly CorrectionCandidate[] Items;
	public int Count { get; private set; }
	public readonly int Capacity;
	private readonly float _absoluteMax;

	public TopCandidates(int capacity, float absoluteMax)
	{
		Capacity = capacity;
		Items = new CorrectionCandidate[capacity];
		_absoluteMax = absoluteMax;
	}

	public float BoundingDistance => Count < Capacity ? _absoluteMax : Items[Count - 1].Distance;

	public void TryAdd(float distance, byte frequency, ReadOnlySpan<char> form, MorphologyTagset[] tagsets)
	{
		if (Count >= Capacity)
		{
			var worst = Items[Capacity - 1];
			if (distance > worst.Distance || (distance == worst.Distance && frequency <= worst.Frequency))
			{
				return;
			}
		}

		for (int i = 0; i < Count; i++)
		{
			if (form.SequenceEqual(Items[i].Word.AsSpan())) return;
		}

		var candidate = new CorrectionCandidate(form.ToString(), distance, frequency, tagsets);

		if (Count < Capacity)
		{
			Items[Count++] = candidate;
		}
		else
		{
			Items[Capacity - 1] = candidate;
		}

		Array.Sort(Items, 0, Count);
	}
}