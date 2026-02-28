using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Pero.Kernel.Fuzzy;

public abstract class BasePenaltyMatrix
{
	private readonly float[] _subCosts;
	private readonly int _alphabetSize;

	public IReadOnlyList<char> Alphabet { get; }

	protected BasePenaltyMatrix(IReadOnlyList<char> alphabet)
	{
		Alphabet = alphabet;
		_alphabetSize = alphabet.Count;
		_subCosts = new float[_alphabetSize * _alphabetSize];
		Precompute();
	}

	private void Precompute()
	{
		for (int i = 0; i < _alphabetSize; i++)
		{
			for (int j = 0; j < _alphabetSize; j++)
			{
				_subCosts[i * _alphabetSize + j] = CalculateSubstitutionCost(Alphabet[i], Alphabet[j]);
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float GetSubstitutionCostUnsafe(char expected, char actual)
	{
		if (expected == actual) return 0f;

		int i1 = CharToIndex(expected);
		int i2 = CharToIndex(actual);

		if (i1 < 0 || i2 < 0) return 1.0f;

		return Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(_subCosts), i1 * _alphabetSize + i2);
	}

	protected abstract float CalculateSubstitutionCost(char expected, char actual);
	protected abstract int CharToIndex(char c);

	public abstract float GetInsertionCost(char c);
	public abstract float GetDeletionCost(char c);
	public abstract float GetPositionalMultiplier(int currentIndex, int wordLength);
}