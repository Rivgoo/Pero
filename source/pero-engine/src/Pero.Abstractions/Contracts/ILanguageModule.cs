namespace Pero.Abstractions.Contracts;

/// <summary>
/// The main entry point for a specific language's implementation.
/// It acts as a factory for creating all language-specific components of the pipeline.
/// </summary>
public interface ILanguageModule
{
	/// <summary>
	/// The unique language code (e.g., "uk-UA").
	/// </summary>
	string LanguageCode { get; }

	/// <summary>
	/// Creates a text cleaner for the language.
	/// </summary>
	ITextCleaner CreateTextCleaner();

	/// <summary>
	/// Creates a pre-tokenizer for the language.
	/// </summary>
	IPreTokenizer CreatePreTokenizer();

	/// <summary>
	/// Creates a tokenizer for the language.
	/// </summary>
	ITokenizer CreateTokenizer();

	/// <summary>
	/// Creates a sentence segmenter for the language.
	/// </summary>
	ISentenceSegmenter CreateSentenceSegmenter();

	/// <summary>
	/// Creates a morphology analyzer for the language.
	/// </summary>
	IMorphologyAnalyzer CreateMorphologyAnalyzer();

	/// <summary>
	/// Gets the collection of analysis rules for the language.
	/// </summary>
	IEnumerable<IRule> GetRules();
}