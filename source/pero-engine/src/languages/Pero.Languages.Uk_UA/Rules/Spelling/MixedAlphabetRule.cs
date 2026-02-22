using Pero.Abstractions.Models;
using Pero.Kernel.Rules;
using Pero.Kernel.Utils;

namespace Pero.Languages.Uk_UA.Rules.Spelling;

public class MixedAlphabetRule : BaseGrammarRule
{
	public override string Id => "UK_UA_SPELLING_MIXED_ALPHABETS";
	public override IssueCategory Category => IssueCategory.Spelling;
	public override IssueSeverity Severity => IssueSeverity.Warning;

	private static readonly Dictionary<char, char> HomoglyphMap = new()
	{
		{ 'A', 'А' }, { 'a', 'а' },
		{ 'B', 'В' },
		{ 'C', 'С' }, { 'c', 'с' },
		{ 'E', 'Е' }, { 'e', 'е' },
		{ 'H', 'Н' },
		{ 'I', 'І' }, { 'i', 'і' },
		{ 'K', 'К' },
		{ 'M', 'М' },
		{ 'O', 'О' }, { 'o', 'о' },
		{ 'P', 'Р' }, { 'p', 'р' },
		{ 'T', 'Т' },
		{ 'X', 'Х' }, { 'x', 'х' },
		{ 'y', 'у' }
	};

	protected override IEnumerable<TextIssue> Analyze(Sentence sentence)
	{
		foreach (var token in sentence.Tokens)
		{
			if (IsTechnical(token) || token.Type != TokenType.Word) continue;

			if (HasMixedAlphabets(token.Text, out bool isFixable))
			{
				var suggestions = new List<string>();

				if (isFixable)
				{
					string fixedWord = ReplaceHomoglyphs(token.Text);
					suggestions.Add(fixedWord);
				}

				yield return IssueFactory.CreateFrom(token, Id, Category, suggestions);
			}
		}
	}

	private static bool HasMixedAlphabets(string word, out bool isFixable)
	{
		bool hasCyrillic = false;
		bool hasLatin = false;
		bool containsOnlyKnownHomoglyphs = true;

		foreach (char c in word)
		{
			if (!char.IsLetter(c)) continue;

			if ((c >= '\u0400' && c <= '\u04FF') || (c >= '\u0500' && c <= '\u052F'))
			{
				hasCyrillic = true;
			}

			else if ((c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z'))
			{
				hasLatin = true;
				if (!HomoglyphMap.ContainsKey(c))
				{
					containsOnlyKnownHomoglyphs = false;
				}
			}
		}

		bool isMixed = hasCyrillic && hasLatin;
		isFixable = isMixed && containsOnlyKnownHomoglyphs;

		return isMixed;
	}

	private static string ReplaceHomoglyphs(string mixedWord)
	{
		var chars = mixedWord.ToCharArray();
		for (int i = 0; i < chars.Length; i++)
		{
			if (HomoglyphMap.TryGetValue(chars[i], out char cyrillicChar))
			{
				chars[i] = cyrillicChar;
			}
		}
		return new string(chars);
	}
}