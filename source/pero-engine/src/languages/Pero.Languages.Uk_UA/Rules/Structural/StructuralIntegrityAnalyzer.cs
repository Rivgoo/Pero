using Pero.Abstractions.Contracts;
using Pero.Abstractions.Models;
using Pero.Kernel.Rules;
using Pero.Kernel.Utils;

namespace Pero.Languages.Uk_UA.Rules.Structural;

public class StructuralIntegrityAnalyzer : BaseAnalyzer
{
	public override string Name => "StructuralIntegrity";

	private const string UnmatchedOpenId = "UK_UA_STRUCTURAL_UNMATCHED_OPEN";
	private const string UnpairedQuoteId = "UK_UA_STRUCTURAL_UNPAIRED_QUOTE";

	private static readonly Dictionary<char, char> BracketPairs = new()
	{
		{ '(', ')' }, { '[', ']' }, { '{', '}' },
		{ '«', '»' }, { '“', '”' }, { '‘', '’' }, { '‹', '›' }
	};

	private static readonly HashSet<char> OpenBrackets = new(BracketPairs.Keys);
	private static readonly HashSet<char> CloseBrackets = new(BracketPairs.Values);

	public override IReadOnlyCollection<RuleDefinition> SupportedRules { get; } = new List<RuleDefinition>
	{
		new(UnmatchedOpenId, IssueCategory.Typography, IssueSeverity.Warning),
		new(UnpairedQuoteId, IssueCategory.Typography, IssueSeverity.Warning),
	};

	protected override IEnumerable<TextIssue> Execute(Sentence sentence)
	{
		var bracketStack = new Stack<(char OpenChar, Token Token)>();
		var doubleQuotes = new List<Token>();
		var singleQuotes = new List<Token>();

		foreach (var token in sentence.Tokens)
		{
			if (token.Type != TokenType.Punctuation && token.Type != TokenType.Symbol)
			{
				continue;
			}

			if (token.Text.Length == 1)
			{
				ProcessCharToken(token, sentence, bracketStack, doubleQuotes, singleQuotes);
			}
		}

		foreach (var issue in FinalizeBracketChecks(bracketStack))
		{
			yield return issue;
		}

		foreach (var issue in FinalizeQuoteChecks(doubleQuotes, singleQuotes, sentence))
		{
			yield return issue;
		}
	}

	private void ProcessCharToken(
		Token token,
		Sentence sentence,
		Stack<(char OpenChar, Token Token)> bracketStack,
		List<Token> doubleQuotes,
		List<Token> singleQuotes)
	{
		char c = token.Text[0];

		if (OpenBrackets.Contains(c))
		{
			bracketStack.Push((c, token));
		}
		else if (CloseBrackets.Contains(c))
		{
			if (bracketStack.TryPeek(out var top) && BracketPairs[top.OpenChar] == c)
			{
				bracketStack.Pop();
			}
		}
		else if (c == '"')
		{
			doubleQuotes.Add(token);
		}
		else if (c == '\'')
		{
			if (IsLikelyQuote(token, sentence))
			{
				singleQuotes.Add(token);
			}
		}
	}

	private IEnumerable<TextIssue> FinalizeBracketChecks(Stack<(char OpenChar, Token Token)> stack)
	{
		while (stack.Count > 0)
		{
			var unclosed = stack.Pop();
			var args = new Dictionary<string, string> { { "char", unclosed.OpenChar.ToString() } };

			yield return CreateIssueWithArgs(unclosed.Token, UnmatchedOpenId, args, string.Empty);
		}
	}

	private IEnumerable<TextIssue> FinalizeQuoteChecks(List<Token> doubleQuotes, List<Token> singleQuotes, Sentence sentence)
	{
		if (doubleQuotes.Count % 2 != 0)
		{
			var lastQuote = doubleQuotes.Last();
			var args = new Dictionary<string, string> { { "char", "\"" } };

			var suggestion = GuessCorrectQuote(lastQuote, sentence, "«", "»");
			yield return CreateIssueWithArgs(lastQuote, UnpairedQuoteId, args, suggestion, string.Empty);
		}

		if (singleQuotes.Count % 2 != 0)
		{
			var lastQuote = singleQuotes.Last();
			var args = new Dictionary<string, string> { { "char", "'" } };

			var suggestion = GuessCorrectQuote(lastQuote, sentence, "‘", "’");
			yield return CreateIssueWithArgs(lastQuote, UnpairedQuoteId, args, suggestion, string.Empty);
		}
	}

	private string GuessCorrectQuote(Token quoteToken, Sentence sentence, string openVariant, string closeVariant)
	{
		var prev = sentence.GetPreviousToken(quoteToken);
		var next = sentence.GetNextToken(quoteToken);

		bool spaceBefore = prev == null || prev.Type == TokenType.Whitespace;
		bool spaceAfter = next == null || next.Type == TokenType.Whitespace;

		if (spaceBefore && !spaceAfter) return openVariant;

		if (!spaceBefore && spaceAfter) return closeVariant;

		if (!spaceBefore && next?.Type == TokenType.Punctuation) return closeVariant;

		return openVariant;
	}

	private bool IsLikelyQuote(Token token, Sentence sentence)
	{
		var prev = sentence.GetPreviousToken(token);
		var next = sentence.GetNextToken(token);

		bool insideWord = (prev?.Type == TokenType.Word && next?.Type == TokenType.Word);
		return !insideWord;
	}

	private TextIssue CreateIssueWithArgs(Token token, string ruleId, Dictionary<string, string> args, params string[] suggestions)
	{
		var rule = GetRule(ruleId);

		return new TextIssue
		{
			RuleId = rule.Id,
			Category = rule.Category,
			Severity = rule.Severity,
			Start = token.Start,
			End = token.End,
			Original = token.Text,
			Suggestions = suggestions.ToList(),
			MessageArgs = args
		};
	}
}