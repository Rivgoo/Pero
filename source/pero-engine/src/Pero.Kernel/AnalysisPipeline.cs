using Pero.Abstractions.Contracts;
using Pero.Abstractions.Models;

namespace Pero.Kernel.Pipeline;

/// <summary>
/// Orchestrates the multi-stage text analysis process for a specific language.
/// This class is designed to be stateless and created per analysis request.
/// </summary>
public class AnalysisPipeline
{
	private readonly ILanguageModule _module;

	public AnalysisPipeline(ILanguageModule module)
	{
		_module = module;
	}

	/// <summary>
	/// Executes the full analysis pipeline on the given text.
	/// </summary>
	public AnalysisResult Run(string rawText)
	{
		var cleaner = _module.CreateTextCleaner();
		var preTokenizer = _module.CreatePreTokenizer();
		var tokenizer = _module.CreateTokenizer();
		var segmenter = _module.CreateSentenceSegmenter();
		var morphAnalyzer = _module.CreateMorphologyAnalyzer();
		var rules = _module.GetRules().ToList();

		var cleanedText = cleaner.Clean(rawText);
		var fragments = preTokenizer.Scan(cleanedText);
		var allTokens = ProcessFragments(fragments, tokenizer);
		var sentences = segmenter.Segment(allTokens).ToList();

		foreach (var sentence in sentences)
		{
			morphAnalyzer.Enrich(sentence);
		}

		var allIssues = new List<TextIssue>();
		foreach (var sentence in sentences)
		{
			foreach (var rule in rules)
			{
				allIssues.AddRange(rule.Check(sentence));
			}
		}

		var document = new AnalyzedDocument(rawText, sentences);
		return new AnalysisResult(document, allIssues);
	}

	private static List<Token> ProcessFragments(IEnumerable<TextFragment> fragments, ITokenizer tokenizer)
	{
		var tokens = new List<Token>();
		foreach (var fragment in fragments)
		{
			if (fragment.Type == FragmentType.Raw)
			{
				tokens.AddRange(tokenizer.Tokenize(fragment));
			}
			else
			{
				tokens.Add(new Token(
					text: fragment.Text,
					normalizedText: fragment.Text,
					type: MapFragmentTypeToTokenType(fragment.Type),
					start: fragment.Start,
					end: fragment.End
				));
			}
		}
		return tokens;
	}

	private static TokenType MapFragmentTypeToTokenType(FragmentType fragmentType) => fragmentType switch
	{
		FragmentType.Url => TokenType.Url,
		FragmentType.Email => TokenType.Email,
		FragmentType.CodeSnippet => TokenType.CodeSnippet,
		FragmentType.Mention => TokenType.Mention,
		FragmentType.Date => TokenType.Date,
		FragmentType.Time => TokenType.Time,
		FragmentType.Currency => TokenType.Currency,
		FragmentType.FilePath => TokenType.FilePath,
		FragmentType.IpAddress => TokenType.IpAddress,
		FragmentType.MacAddress => TokenType.MacAddress,
		FragmentType.PhoneNumber => TokenType.PhoneNumber,
		FragmentType.VersionNumber => TokenType.VersionNumber,
		FragmentType.Guid => TokenType.Guid,
		FragmentType.HexColor => TokenType.HexColor,
		FragmentType.MarkdownFormat => TokenType.MarkdownFormat,
		FragmentType.Coordinates => TokenType.Coordinates,
		FragmentType.CryptoWalletAddress => TokenType.CryptoWalletAddress,
		FragmentType.SocialMediaHandle => TokenType.SocialMediaHandle,
		FragmentType.Dimensions => TokenType.Dimensions,

		_ => TokenType.Unknown
	};
}