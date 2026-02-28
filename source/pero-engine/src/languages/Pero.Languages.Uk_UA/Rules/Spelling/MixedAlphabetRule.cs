using Pero.Abstractions.Models;
using Pero.Kernel.Rules;
using Pero.Kernel.Utils;

namespace Pero.Languages.Uk_UA.Rules.Spelling;

public class MixedAlphabetRule : BaseGrammarRule
{
	public override string Id => "UK_UA_SPELLING_MIXED_ALPHABETS";
	public override IssueCategory Category => IssueCategory.Spelling;
	public override IssueSeverity Severity => IssueSeverity.Warning;

	private static readonly Dictionary<char, char> Homoglyphs = new()
	{
		{'A', 'А'}, {'a', 'а'}, {'B', 'В'}, {'b', 'в'}, {'C', 'С'}, {'c', 'с'},
		{'E', 'Е'}, {'e', 'е'}, {'H', 'Н'}, {'i', 'і'}, {'I', 'І'}, {'K', 'К'},
		{'k', 'к'}, {'M', 'М'}, {'O', 'О'}, {'o', 'о'}, {'P', 'Р'}, {'p', 'р'},
		{'T', 'Т'}, {'X', 'Х'}, {'x', 'х'}, {'y', 'у'}, {'Y', 'У'}
	};

	private static readonly Dictionary<char, char> Layout = new()
	{
		{'q', 'й'}, {'w', 'ц'}, {'e', 'у'}, {'r', 'к'}, {'t', 'е'}, {'y', 'н'}, {'u', 'г'}, {'i', 'ш'}, {'o', 'щ'}, {'p', 'з'}, {'[', 'х'}, {']', 'ї'},
		{'a', 'ф'}, {'s', 'і'}, {'d', 'в'}, {'f', 'а'}, {'g', 'п'}, {'h', 'р'}, {'j', 'о'}, {'k', 'л'}, {'l', 'д'}, {';', 'ж'}, {'\'', 'є'},
		{'z', 'я'}, {'x', 'ч'}, {'c', 'с'}, {'v', 'м'}, {'b', 'и'}, {'n', 'т'}, {'m', 'ь'}, {',', 'б'}, {'.', 'ю'},
		{'Q', 'Й'}, {'W', 'Ц'}, {'E', 'У'}, {'R', 'К'}, {'T', 'Е'}, {'Y', 'Н'}, {'U', 'Г'}, {'I', 'Ш'}, {'O', 'Щ'}, {'P', 'З'}, {'{', 'Х'}, {'}', 'Ї'},
		{'A', 'Ф'}, {'S', 'І'}, {'D', 'В'}, {'F', 'А'}, {'G', 'П'}, {'H', 'Р'}, {'J', 'О'}, {'K', 'Л'}, {'L', 'Д'}, {':', 'Ж'}, {'"', 'Є'},
		{'Z', 'Я'}, {'X', 'Ч'}, {'C', 'С'}, {'V', 'М'}, {'B', 'И'}, {'N', 'Т'}, {'M', 'Ь'}, {'<', 'Б'}, {'>', 'Ю'}
	};

	protected override IEnumerable<TextIssue> Analyze(Sentence sentence)
	{
		foreach (var token in sentence.Tokens)
		{
			if (IsTechnical(token) || token.Type != TokenType.Word) continue;

			var suggestions = GetFixes(token.Text);
			if (suggestions.Count > 0)
			{
				yield return IssueFactory.CreateFrom(token, Id, Category, suggestions);
			}
		}
	}

	private static List<string> GetFixes(string word)
	{
		bool hasCyrillic = false;
		bool hasLatin = false;

		foreach (char c in word)
		{
			if (IsCyrillic(c)) hasCyrillic = true;
			else if (IsLatin(c)) hasLatin = true;
		}

		var suggestions = new List<string>();

		if (hasCyrillic && hasLatin)
		{
			bool canFixHomoglyphs = true;
			char[] homoglyphFix = word.ToCharArray();

			for (int i = 0; i < homoglyphFix.Length; i++)
			{
				if (IsLatin(homoglyphFix[i]))
				{
					if (Homoglyphs.TryGetValue(homoglyphFix[i], out char ukrChar))
					{
						homoglyphFix[i] = ukrChar;
					}
					else
					{
						canFixHomoglyphs = false;
						break;
					}
				}
			}

			if (canFixHomoglyphs)
			{
				suggestions.Add(new string(homoglyphFix));
			}

			char[] layoutFix = word.ToCharArray();
			bool layoutChanged = false;

			for (int i = 0; i < layoutFix.Length; i++)
			{
				if (IsLatin(layoutFix[i]))
				{
					if (Layout.TryGetValue(layoutFix[i], out char ukrChar))
					{
						layoutFix[i] = ukrChar;
						layoutChanged = true;
					}
				}
			}

			if (layoutChanged)
			{
				string lFix = new string(layoutFix);
				if (!suggestions.Contains(lFix))
				{
					suggestions.Add(lFix);
				}
			}
		}

		return suggestions;
	}

	private static bool IsCyrillic(char c) => (c >= '\u0400' && c <= '\u04FF') || (c >= '\u0500' && c <= '\u052F');
	private static bool IsLatin(char c) => (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z');
}