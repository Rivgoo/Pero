namespace Pero.Kernel.Fuzzy;

public class MatcherContext
{
	public float[] PositionMultipliers { get; } = new float[64];
	public float[] InsertionCosts { get; } = new float[64];
	public BasePenaltyMatrix PenaltyMatrix { get; }

	public MatcherContext(BasePenaltyMatrix penaltyMatrix)
	{
		PenaltyMatrix = penaltyMatrix;
	}

	public void InitializeForWord(string word)
	{
		int len = word.Length;
		for (int i = 0; i < len; i++)
		{
			PositionMultipliers[i] = PenaltyMatrix.GetPositionalMultiplier(i, len);
			InsertionCosts[i] = PenaltyMatrix.GetInsertionCost(word[i]);
		}
	}
}