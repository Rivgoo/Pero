using Pero.Abstractions.Contracts;
using Pero.Abstractions.Models;
using Pero.Kernel.Rules;

namespace Pero.Languages.Uk_UA.Rules.Typography;

public class CaseAndCapitalizationAnalyzer : BaseAnalyzer
{
	public override string Name => "CaseAndCapitalization";

	private const string SentenceStartId = "UK_UA_CASE_SENTENCE_START";
	private const string MixedCaseId = "UK_UA_CASE_MIXED";
	private const string InvertedCapsId = "UK_UA_CASE_INVERTED";

	public override IReadOnlyCollection<RuleDefinition> SupportedRules { get; } = new List<RuleDefinition>
	{
		new(SentenceStartId, IssueCategory.Style, IssueSeverity.Warning),
		new(MixedCaseId, IssueCategory.Style, IssueSeverity.Warning),
		new(InvertedCapsId, IssueCategory.Style, IssueSeverity.Warning)
	};

	protected override IEnumerable<TextIssue> Execute(Sentence sentence)
	{
		bool isFirstWordFound = false;

		foreach (var token in sentence.Tokens)
		{
			if (token.Type != TokenType.Word) continue;

			bool isFirstWord = !isFirstWordFound;
			isFirstWordFound = true;

			string text = token.Text;
			if (text.Length == 0) continue;

			// 1. Inverted Case: "пРИВІТ" -> "Привіт"
			if (IsInvertedCase(text))
			{
				string suggestion = ToCapitalized(text);
				yield return CreateIssue(token, InvertedCapsId, suggestion);
				continue;
			}

			// 2. Mixed Case: "ПрИвІт" -> "Привіт" / "привіт"
			if (IsMixedCase(text))
			{
				string lower = token.NormalizedText;
				string capitalized = ToCapitalized(text);
				string suggestion = isFirstWord ? capitalized : lower;

				yield return CreateIssue(token, MixedCaseId, suggestion);
				continue;
			}

			// 3. Sentence Start: "привіт" -> "Привіт"
			if (isFirstWord && IsAllLower(text))
			{
				// Exception for known lowercase brands could be added here if needed (e.g. iPhone)
				// For now, standard grammar dictates capitalization.
				string suggestion = ToCapitalized(text);
				yield return CreateIssue(token, SentenceStartId, suggestion);
			}
		}
	}

	private static bool IsInvertedCase(string text)
	{
		// Requires at least 2 chars to detect inversion (e.g. "тВ")
		if (text.Length < 2) return false;

		// First char is lower
		if (!char.IsLower(text[0])) return false;

		// All subsequent letters are upper
		for (int i = 1; i < text.Length; i++)
		{
			if (!char.IsUpper(text[i])) return false;
		}

		return true;
	}

	private static bool IsMixedCase(string text)
	{
		if (text.Length < 2) return false;

		bool hasUpper = false;
		bool hasLower = false;
		bool firstIsUpper = char.IsUpper(text[0]);

		// Check internal characters (skip first)
		for (int i = 1; i < text.Length; i++)
		{
			if (char.IsUpper(text[i])) hasUpper = true;
			else if (char.IsLower(text[i])) hasLower = true;
		}

		// Valid patterns:
		// 1. All Lower (hasLower=true, hasUpper=false, firstIsUpper=false) -> OK
		// 2. All Upper (hasLower=false, hasUpper=true, firstIsUpper=true) -> OK
		// 3. Capitalized (hasLower=true, hasUpper=false, firstIsUpper=true) -> OK

		// Invalid:
		// - Lower then Upper inside (camelCase) -> "Mixed"
		// - Upper then Lower inside (unusual unless CamelCase) -> "Mixed"

		if (firstIsUpper)
		{
			// Example: "BiG", "HeLlo" -> Mixed
			// If we have mixed case internally, it's weird.
			// "McDonalds" is an exception, but rare in UA.
			return hasUpper && hasLower;
		}
		else
		{
			// Example: "iPhone" -> technically mixed, but valid brand.
			// Example: "hELLO" -> Inverted (handled separately).
			// Example: "hEllo" -> Mixed.
			return hasUpper;
		}
	}

	private static bool IsAllLower(string text)
	{
		foreach (char c in text)
		{
			if (char.IsLetter(c) && char.IsUpper(c)) return false;
		}
		return true;
	}

	private static string ToCapitalized(string text)
	{
		// Convert whole string to lower first to fix "пРИВІТ" -> "привіт" -> "Привіт"
		var lower = text.ToLowerInvariant();
		if (lower.Length > 0)
		{
			return char.ToUpperInvariant(lower[0]) + lower.Substring(1);
		}
		return lower;
	}
}