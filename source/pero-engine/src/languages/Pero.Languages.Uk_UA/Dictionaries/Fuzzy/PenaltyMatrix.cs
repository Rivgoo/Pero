using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Pero.Languages.Uk_UA.Dictionaries.Fuzzy;

public static class PenaltyMatrix
{
	private const float MaxKeyboardDistance = 10.5f;
	private static readonly float[] SubCosts = new float[1600];

	public static readonly char[] UkrainianAlphabet =
	{
		'а', 'б', 'в', 'г', 'ґ', 'д', 'е', 'є', 'ж', 'з', 'и', 'і', 'ї', 'й', 'к',
		'л', 'м', 'н', 'о', 'п', 'р', 'с', 'т', 'у', 'ф', 'х', 'ц', 'ч', 'ш', 'щ',
		'ь', 'ю', 'я', '\''
	};

	static PenaltyMatrix()
	{
		for (int i = 0; i < 40; i++)
		{
			for (int j = 0; j < 40; j++)
			{
				SubCosts[i * 40 + j] = CalculateSubstitutionCost(IndexToChar(i), IndexToChar(j));
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float GetSubstitutionCostUnsafe(char expected, char actual)
	{
		if (expected == actual) return 0f;
		int i1 = CharToIndex(expected);
		int i2 = CharToIndex(actual);
		if (i1 < 0 || i2 < 0) return 1.0f;
		return Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(SubCosts), i1 * 40 + i2);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float GetInsertionCost(char c)
	{
		if (IsApostrophe(c)) return 0.1f;
		if (c == 'ь' || c == 'й') return 0.4f;
		return IsVowel(c) ? 0.7f : 0.95f;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float GetDeletionCost(char c)
	{
		if (IsApostrophe(c)) return 0.1f;
		if (c == 'ь' || c == 'й') return 0.4f;
		return 0.95f;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float GetPositionalMultiplier(int currentIndex, int wordLength)
	{
		if (currentIndex == 0) return 1.2f;
		if (currentIndex >= wordLength - 2) return 0.8f;
		return 1.0f;
	}

	private static float CalculateSubstitutionCost(char expected, char actual)
	{
		if (IsPhoneticPair(expected, actual)) return 0.2f;

		var (x1, y1) = GetCoordinates(expected);
		var (x2, y2) = GetCoordinates(actual);

		if (y1 < 0 || y2 < 0) return 1.0f;

		float distance = (float)Math.Sqrt(Math.Pow(x1 - x2, 2) + Math.Pow(y1 - y2, 2));

		return 0.15f + (0.85f * (distance / MaxKeyboardDistance));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static int CharToIndex(char c)
	{
		if (c >= 'а' && c <= 'я') return c - 'а';
		return c switch { 'і' => 32, 'ї' => 33, 'є' => 34, 'ґ' => 35, '\'' or '’' or 'ʼ' => 36, _ => -1 };
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static char IndexToChar(int index)
	{
		if (index >= 0 && index <= 31) return (char)('а' + index);
		return index switch { 32 => 'і', 33 => 'ї', 34 => 'є', 35 => 'ґ', 36 => '\'', _ => '\0' };
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool IsPhoneticPair(char a, char b)
	{
		return (a == 'е' && b == 'и') || (a == 'и' && b == 'е') ||
			   (a == 'г' && b == 'ґ') || (a == 'ґ' && b == 'г') ||
			   (a == 'і' && b == 'ї') || (a == 'ї' && b == 'і') ||
			   (a == 'о' && b == 'а') || (a == 'а' && b == 'о') ||
			   (a == 'ш' && b == 'ж') || (a == 'ж' && b == 'ш') ||
			   (a == 'з' && b == 'с') || (a == 'с' && b == 'з') ||
			   (a == 'т' && b == 'д') || (a == 'д' && b == 'т') ||
			   (a == 'п' && b == 'б') || (a == 'б' && b == 'п') ||
			   (a == 'ц' && b == 'т') || (a == 'т' && b == 'ц') ||
			   (a == 'ш' && b == 'ч') || (a == 'ч' && b == 'ш') ||
			   (a == 'н' && b == 'м') || (a == 'м' && b == 'н');
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool IsVowel(char c) => c is 'а' or 'е' or 'є' or 'и' or 'і' or 'ї' or 'о' or 'у' or 'ю' or 'я';

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool IsApostrophe(char c) => c is '\'' or '’' or 'ʼ';

	private static (float X, float Y) GetCoordinates(char c) => c switch
	{
		'й' => (0f, 0f),
		'ц' => (1f, 0f),
		'у' => (2f, 0f),
		'к' => (3f, 0f),
		'е' => (4f, 0f),
		'н' => (5f, 0f),
		'г' => (6f, 0f),
		'ш' => (7f, 0f),
		'щ' => (8f, 0f),
		'з' => (9f, 0f),
		'х' => (10f, 0f),
		'ї' => (11f, 0f),
		'ф' => (0.5f, 1f),
		'і' => (1.5f, 1f),
		'в' => (2.5f, 1f),
		'а' => (3.5f, 1f),
		'п' => (4.5f, 1f),
		'р' => (5.5f, 1f),
		'о' => (6.5f, 1f),
		'л' => (7.5f, 1f),
		'д' => (8.5f, 1f),
		'ж' => (9.5f, 1f),
		'є' => (10.5f, 1f),
		'я' => (1f, 2f),
		'ч' => (2f, 2f),
		'с' => (3f, 2f),
		'м' => (4f, 2f),
		'и' => (5f, 2f),
		'т' => (6f, 2f),
		'ь' => (7f, 2f),
		'б' => (8f, 2f),
		'ю' => (9f, 2f),
		'ґ' => (11.5f, 1f),
		'\'' or '’' or 'ʼ' => (0f, 1f),
		_ => (-1f, -1f)
	};
}