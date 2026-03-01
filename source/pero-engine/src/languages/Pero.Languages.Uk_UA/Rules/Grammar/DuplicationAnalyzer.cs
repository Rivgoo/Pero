using System.Text;
using Pero.Abstractions.Contracts;
using Pero.Abstractions.Models;
using Pero.Kernel.Rules;

namespace Pero.Languages.Uk_UA.Rules.Grammar;

public class DuplicationAnalyzer : BaseAnalyzer
{
	public override string Name => "Duplication";

	private const string PhraseRuleId = "UK_UA_DUPLICATION_PHRASE";
	private const string WordRuleId = "UK_UA_DUPLICATION_WORD";

	private static readonly HashSet<string> ValidReduplications = new(StringComparer.OrdinalIgnoreCase)
	{
		"ледь", "ледве", "тільки", "ось", "ген", "геть", "так", "ні",
		"довго", "давно", "мало", "багато", "тихо", "гучно", "високо", "низько",
		"далеко", "близько", "глибоко", "мілко", "швидко", "повільно", "чисто",
		"рівно", "часто", "рідко", "гірко", "солодко", "тепло", "холодно", "хутко", "скоро",

		"сам", "сама", "само", "самі",
		"один", "одна", "одно", "одне", "одні",
		"свій", "своя", "своє", "свої", "весь", "все", "всі",

		"леле", "ой", "ай", "ох", "ех", "ах", "ого", "овва", "ну", "тю",
		"ку", "тук", "гав", "няв", "му", "бе", "ме", "ква", "кар", "цвірінь",

		"білий", "біла", "біле", "білі",
		"чорний", "чорна", "чорне", "чорні",
		"синій", "синя", "синє", "сині",
		"червоний", "червона", "червоне", "червоні",
		"зелений", "зелена", "зелене", "зелені",
		"старий", "стара", "старе", "старі",
		"новий", "нова", "нове", "нові",
		"малий", "мала", "мале", "малі",
		"великий", "велика", "велике", "великі",
		"добрий", "добра", "добре", "добрі",
		"дикий", "дика", "дике", "дикі",
		"святий", "свята", "святе", "святі",
		"чистий", "чиста", "чисте", "чисті",

		"іду", "йду", "іде", "йде", "чекаю", "чекає", "дивлюсь", "дивиться",
		"плачу", "плаче", "лечу", "летить", "росту", "росте", "біжу", "біжить"
	};

	public override IReadOnlyCollection<RuleDefinition> SupportedRules { get; } = new List<RuleDefinition>
	{
		new(PhraseRuleId, IssueCategory.Grammar, IssueSeverity.Warning),
		new(WordRuleId, IssueCategory.Grammar, IssueSeverity.Warning)
	};

	protected override IEnumerable<TextIssue> Execute(Sentence sentence)
	{
		var tokens = sentence.Tokens;
		int skipUntilIndex = -1;

		for (int i = 0; i < tokens.Count; i++)
		{
			if (i <= skipUntilIndex) continue;

			var token = tokens[i];

			if (token.Type != TokenType.Word) continue;

			var phraseResult = CheckPhraseDuplication(tokens, i);
			if (phraseResult.Consumed > 0)
			{
				if (phraseResult.Issue != null) yield return phraseResult.Issue;
				skipUntilIndex = i + phraseResult.Consumed - 1;
				continue;
			}

			var wordResult = CheckWordDuplication(tokens, i);
			if (wordResult.Consumed > 0)
			{
				if (wordResult.Issue != null) yield return wordResult.Issue;
				skipUntilIndex = i + wordResult.Consumed - 1;
			}
		}
	}

	private (TextIssue? Issue, int Consumed) CheckPhraseDuplication(IReadOnlyList<Token> tokens, int startIndex)
	{
		var (t2, idx2) = GetNextSignificant(tokens, startIndex);
		if (t2 == null || t2.Type != TokenType.Word) return (null, 0);

		var (t3, idx3) = GetNextSignificant(tokens, idx2);
		if (t3 == null || t3.Type != TokenType.Word) return (null, 0);

		var (t4, idx4) = GetNextSignificant(tokens, idx3);
		if (t4 == null || t4.Type != TokenType.Word) return (null, 0);

		var t1 = tokens[startIndex];

		if (string.Equals(t1.NormalizedText, t3.NormalizedText, StringComparison.Ordinal) &&
			string.Equals(t2.NormalizedText, t4.NormalizedText, StringComparison.Ordinal))
		{
			string suggestion = ExtractOriginalText(tokens, startIndex, idx2);
			var chunk = tokens.Skip(startIndex).Take(idx4 - startIndex + 1).ToList();

			return (CreateIssue(chunk, PhraseRuleId, suggestion), idx4 - startIndex + 1);
		}

		return (null, 0);
	}

	private (TextIssue? Issue, int Consumed) CheckWordDuplication(IReadOnlyList<Token> tokens, int startIndex)
	{
		var t1 = tokens[startIndex];
		var (t2, idx2) = GetNextSignificant(tokens, startIndex);

		if (t2 == null || t2.Type != TokenType.Word) return (null, 0);

		if (string.Equals(t1.NormalizedText, t2.NormalizedText, StringComparison.Ordinal))
		{
			if (ValidReduplications.Contains(t1.NormalizedText)) return (null, 0);

			if (t1.Text.Length == 1) return (null, 0);

			var separatorToken = tokens[startIndex + 1];
			if (separatorToken.Text.Contains("-") || separatorToken.Text.Contains("—"))
			{
				return (null, 0);
			}

			string suggestion = t1.Text;
			var chunk = tokens.Skip(startIndex).Take(idx2 - startIndex + 1).ToList();

			return (CreateIssue(chunk, WordRuleId, suggestion), idx2 - startIndex + 1);
		}

		return (null, 0);
	}

	private static (Token? Token, int Index) GetNextSignificant(IReadOnlyList<Token> tokens, int currentIndex)
	{
		for (int i = currentIndex + 1; i < tokens.Count; i++)
		{
			if (tokens[i].Type != TokenType.Whitespace)
			{
				return (tokens[i], i);
			}
		}
		return (null, -1);
	}

	private static string ExtractOriginalText(IReadOnlyList<Token> tokens, int start, int end)
	{
		var builder = new StringBuilder();
		for (int i = start; i <= end; i++)
		{
			builder.Append(tokens[i].Text);
		}
		return builder.ToString();
	}
}