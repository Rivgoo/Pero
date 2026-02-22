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
		var spellChecker = _module.CreateSpellChecker();
		var rules = _module.GetRules().ToList();

		var cleanedText = cleaner.Clean(rawText);
		var fragments = preTokenizer.Scan(cleanedText);
		var allTokens = ProcessFragments(fragments, tokenizer);
		var sentences = segmenter.Segment(allTokens).ToList();

		var document = new AnalyzedDocument(rawText, sentences);

		foreach (var sentence in sentences)
		{
			morphAnalyzer.Enrich(sentence);
		}

		var allIssues = new List<TextIssue>();

		allIssues.AddRange(spellChecker.Check(document));

		foreach (var sentence in sentences)
		{
			foreach (var rule in rules)
			{
				allIssues.AddRange(rule.Check(sentence));
			}
		}

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
					type: FragmentTokenMapper.Map(fragment.Type),
					start: fragment.Start,
					end: fragment.End
				));
			}
		}
		return tokens;
	}
}