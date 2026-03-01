using Pero.Abstractions.Contracts;
using Pero.Abstractions.Models;
using Pero.Kernel.Rules;

namespace Pero.Languages.Uk_UA.Rules.Typography;

public class PunctuationAnalyzer : BaseAnalyzer
{
	public override string Name => "Punctuation";

	private const string SpaceBeforePunctuationId = "UK_UA_PUNCT_SPACE_BEFORE";
	private const string MissingSpaceAfterId = "UK_UA_PUNCT_MISSING_SPACE_AFTER";
	private const string DoublePunctuationId = "UK_UA_PUNCT_DOUBLE";
	private const string ExcessiveExclamationId = "UK_UA_PUNCT_EXCESSIVE_EXCLAMATION";
	private const string ExcessiveQuestionId = "UK_UA_PUNCT_EXCESSIVE_QUESTION";
	private const string SpaceBeforeApostropheId = "UK_UA_PUNCT_SPACE_BEFORE_APOSTROPHE";
	private const string SpaceAfterApostropheId = "UK_UA_PUNCT_SPACE_AFTER_APOSTROPHE";
	private const string HyphenInsteadOfDashId = "UK_UA_PUNCT_HYPHEN_FOR_DASH";
	private const string DoubleHyphenId = "UK_UA_PUNCT_DOUBLE_HYPHEN";
	private const string MissingSpaceDashId = "UK_UA_PUNCT_MISSING_SPACE_DASH";
	private const string SpaceInsideQuotesId = "UK_UA_PUNCT_SPACE_INSIDE_QUOTES";
	private const string SpaceInsideParenthesesId = "UK_UA_PUNCT_SPACE_INSIDE_PARENTHESES";
	private const string MissingSpaceBeforeParenthesisId = "UK_UA_PUNCT_MISSING_SPACE_BEFORE_PARENTHESIS";

	private static readonly HashSet<string> TerminalPunctuation = new(StringComparer.Ordinal) { ".", ",", ";", ":" };
	private static readonly HashSet<string> NoSpaceBeforePunctuation = new(StringComparer.Ordinal) { ".", ",", ";", ":", "!", "?", "…" };

	public override IReadOnlyCollection<RuleDefinition> SupportedRules { get; } = new List<RuleDefinition>
	{
		new(SpaceBeforePunctuationId, IssueCategory.Typography, IssueSeverity.Warning),
		new(MissingSpaceAfterId, IssueCategory.Typography, IssueSeverity.Warning),
		new(DoublePunctuationId, IssueCategory.Typography, IssueSeverity.Warning),
		new(ExcessiveExclamationId, IssueCategory.Style, IssueSeverity.Info),
		new(ExcessiveQuestionId, IssueCategory.Style, IssueSeverity.Info),
		new(SpaceBeforeApostropheId, IssueCategory.Typography, IssueSeverity.Warning),
		new(SpaceAfterApostropheId, IssueCategory.Typography, IssueSeverity.Warning),
		new(HyphenInsteadOfDashId, IssueCategory.Typography, IssueSeverity.Warning),
		new(DoubleHyphenId, IssueCategory.Typography, IssueSeverity.Warning),
		new(MissingSpaceDashId, IssueCategory.Typography, IssueSeverity.Warning),
		new(SpaceInsideQuotesId, IssueCategory.Typography, IssueSeverity.Warning),
		new(SpaceInsideParenthesesId, IssueCategory.Typography, IssueSeverity.Warning),
		new(MissingSpaceBeforeParenthesisId, IssueCategory.Typography, IssueSeverity.Warning)
	};

	protected override IEnumerable<TextIssue> Execute(Sentence sentence)
	{
		var tokens = sentence.Tokens;

		for (int i = 0; i < tokens.Count; i++)
		{
			var token = tokens[i];

			if (token.Type != TokenType.Punctuation)
			{
				continue;
			}

			foreach (var issue in CheckApostropheSpacing(tokens, i)) yield return issue;
			foreach (var issue in CheckExcessivePunctuation(token)) yield return issue;
			foreach (var issue in CheckDashUsage(tokens, i)) yield return issue;
			foreach (var issue in CheckPunctuationSpacing(tokens, i)) yield return issue;
			foreach (var issue in CheckBracketSpacing(tokens, i)) yield return issue;
		}
	}

	private IEnumerable<TextIssue> CheckApostropheSpacing(IReadOnlyList<Token> tokens, int i)
	{
		var token = tokens[i];
		if (token.Text is not ("'" or "’" or "ʼ" or "`" or "´"))
		{
			yield break;
		}

		var prev = i > 0 ? tokens[i - 1] : null;
		var next = i + 1 < tokens.Count ? tokens[i + 1] : null;

		if (prev?.Type == TokenType.Whitespace && next?.Type == TokenType.Word)
		{
			var prevWord = i > 1 ? tokens[i - 2] : null;
			if (prevWord?.Type == TokenType.Word)
			{
				yield return CreateIssue(new[] { prevWord, prev, token, next }, SpaceBeforeApostropheId, $"{prevWord.Text}{token.Text}{next.Text}");
			}
		}

		if (prev?.Type == TokenType.Word && next?.Type == TokenType.Whitespace)
		{
			var nextWord = i + 2 < tokens.Count ? tokens[i + 2] : null;
			if (nextWord?.Type == TokenType.Word)
			{
				yield return CreateIssue(new[] { prev, token, next, nextWord }, SpaceAfterApostropheId, $"{prev.Text}{token.Text}{nextWord.Text}");
			}
		}
	}

	private IEnumerable<TextIssue> CheckExcessivePunctuation(Token token)
	{
		if (token.Text.Length <= 1)
		{
			yield break;
		}

		if (token.Text[0] == '!' && token.Text.All(c => c == '!'))
		{
			yield return CreateIssue(token, ExcessiveExclamationId, "!");
		}

		if (token.Text[0] == '?' && token.Text.All(c => c == '?'))
		{
			yield return CreateIssue(token, ExcessiveQuestionId, "?");
		}
	}

	private IEnumerable<TextIssue> CheckDashUsage(IReadOnlyList<Token> tokens, int i)
	{
		var token = tokens[i];

		if (token.Text == "-")
		{
			var prevSpace = i > 0 ? tokens[i - 1] : null;
			var nextSpace = i + 1 < tokens.Count ? tokens[i + 1] : null;

			if (prevSpace?.Type == TokenType.Whitespace && nextSpace?.Type == TokenType.Whitespace)
			{
				var prevWord = i > 1 ? tokens[i - 2] : null;
				var nextWord = i + 2 < tokens.Count ? tokens[i + 2] : null;

				var chunk = new List<Token>();
				if (prevWord != null) chunk.Add(prevWord);
				chunk.Add(prevSpace);
				chunk.Add(token);
				chunk.Add(nextSpace);
				if (nextWord != null) chunk.Add(nextWord);

				string suggestion = (prevWord?.Text ?? string.Empty) + " — " + (nextWord?.Text ?? string.Empty);
				yield return CreateIssue(chunk, HyphenInsteadOfDashId, suggestion);
			}
		}

		if (token.Text == "--")
		{
			yield return CreateIssue(token, DoubleHyphenId, "—");
		}

		if (token.Text == "—")
		{
			var prev = i > 0 ? tokens[i - 1] : null;
			var next = i + 1 < tokens.Count ? tokens[i + 1] : null;

			if (prev?.Type == TokenType.Word && next?.Type == TokenType.Word)
			{
				yield return CreateIssue(new[] { prev, token, next }, MissingSpaceDashId, $"{prev.Text} — {next.Text}");
			}
		}
	}

	private IEnumerable<TextIssue> CheckPunctuationSpacing(IReadOnlyList<Token> tokens, int i)
	{
		var token = tokens[i];

		if (NoSpaceBeforePunctuation.Contains(token.Text))
		{
			if (i > 0 && tokens[i - 1].Type == TokenType.Whitespace)
			{
				var prevWord = i > 1 ? tokens[i - 2] : null;
				if (prevWord != null && prevWord.Type != TokenType.Whitespace)
				{
					yield return CreateIssue(new[] { prevWord, tokens[i - 1], token }, SpaceBeforePunctuationId, $"{prevWord.Text}{token.Text}");
				}
			}
		}

		if (TerminalPunctuation.Contains(token.Text))
		{
			if (i + 1 < tokens.Count)
			{
				var next = tokens[i + 1];
				if (next.Type == TokenType.Word)
				{
					yield return CreateIssue(new[] { token, next }, MissingSpaceAfterId, $"{token.Text} {next.Text}");
				}

				if (next.Type == TokenType.Punctuation && TerminalPunctuation.Contains(next.Text))
				{
					yield return CreateIssue(new[] { token, next }, DoublePunctuationId, token.Text);
				}
			}
		}
	}

	private IEnumerable<TextIssue> CheckBracketSpacing(IReadOnlyList<Token> tokens, int i)
	{
		var token = tokens[i];

		if (token.Text is "(" or "[" or "«" or "“")
		{
			if (i > 0 && tokens[i - 1].Type == TokenType.Word)
			{
				yield return CreateIssue(new[] { tokens[i - 1], token }, MissingSpaceBeforeParenthesisId, $"{tokens[i - 1].Text} {token.Text}");
			}

			if (i + 2 < tokens.Count && tokens[i + 1].Type == TokenType.Whitespace && tokens[i + 2].Type == TokenType.Word)
			{
				string ruleId = token.Text is "«" or "“" ? SpaceInsideQuotesId : SpaceInsideParenthesesId;
				yield return CreateIssue(new[] { token, tokens[i + 1], tokens[i + 2] }, ruleId, $"{token.Text}{tokens[i + 2].Text}");
			}
		}

		if (token.Text is ")" or "]" or "»" or "”")
		{
			if (i > 1 && tokens[i - 1].Type == TokenType.Whitespace && tokens[i - 2].Type == TokenType.Word)
			{
				string ruleId = token.Text is "»" or "”" ? SpaceInsideQuotesId : SpaceInsideParenthesesId;
				yield return CreateIssue(new[] { tokens[i - 2], tokens[i - 1], token }, ruleId, $"{tokens[i - 2].Text}{token.Text}");
			}
		}
	}
}