namespace Pero.Languages.Uk_UA.Tools.Console.Services.Typo.Strategies;

public class PrefixStrategy : ITypoStrategy
{
	private const string CafePtah = "кптфх";

	public bool TryGenerate(string word, Random random, out string typo, out string category)
	{
		if (word.StartsWith("с") && word.Length > 1 && CafePtah.Contains(word[1]))
		{
			typo = "з" + word.Substring(1);
			category = "Prefix З/С Rule Violation";
			return true;
		}

		if (word.StartsWith("з") && word.Length > 1 && !CafePtah.Contains(word[1]))
		{
			typo = "с" + word.Substring(1);
			category = "Prefix З/С Rule Violation";
			return true;
		}

		if (word.StartsWith("пре"))
		{
			typo = "при" + word.Substring(3);
			category = "Prefix Пре/При Substitution";
			return true;
		}

		if (word.StartsWith("при"))
		{
			typo = "пре" + word.Substring(3);
			category = "Prefix Пре/При Substitution";
			return true;
		}

		if (word.StartsWith("роз"))
		{
			typo = "рос" + word.Substring(3);
			category = "Prefix Роз/Рос Surzhyk";
			return true;
		}

		if (word.StartsWith("без"))
		{
			typo = "бес" + word.Substring(3);
			category = "Prefix Без/Бес Surzhyk";
			return true;
		}

		typo = string.Empty;
		category = string.Empty;
		return false;
	}
}
