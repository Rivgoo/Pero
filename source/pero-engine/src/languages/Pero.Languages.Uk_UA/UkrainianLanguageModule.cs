using Pero.Abstractions.Constants;
using Pero.Abstractions.Contracts;
using Pero.Kernel.Components;
using Pero.Languages.Uk_UA.Components;
using Pero.Languages.Uk_UA.Components.Caching;
using Pero.Languages.Uk_UA.Components.Disambiguation;
using Pero.Languages.Uk_UA.Components.Disambiguation.Rules;
using Pero.Languages.Uk_UA.Dictionaries;
using Pero.Languages.Uk_UA.Dictionaries.Fuzzy;
using Pero.Languages.Uk_UA.Resources;
using Pero.Languages.Uk_UA.Rules.Grammar;
using Pero.Languages.Uk_UA.Rules.Spelling;

namespace Pero.Languages.Uk_UA;

/// <summary>
/// The main entry point for the Ukrainian language processing.
/// Acts as a factory for the NLP pipeline components and holds heavy shared resources.
/// </summary>
public class UkrainianLanguageModule : ILanguageModule
{
	private readonly CompiledDictionary _dictionary;
	private readonly LexiconCache _lexicon;
	private readonly FuzzyMatcher _fuzzyMatcher;

	public UkrainianLanguageModule()
	{
		_dictionary = DictionaryProvider.GetDictionary();
		_lexicon = new LexiconCache(_dictionary);
		_fuzzyMatcher = new FuzzyMatcher(_dictionary);
	}

	public string LanguageCode => LanguageCodes.Ukrainian;

	public ITextCleaner CreateTextCleaner()
	{
		return new StandardTextCleaner();
	}

	public IPreTokenizer CreatePreTokenizer()
	{
		return new HeuristicCodePreTokenizer(new StandardPreTokenizer());
	}

	public ITokenizer CreateTokenizer()
	{
		return new UkrainianTokenizer();
	}

	public ISentenceSegmenter CreateSentenceSegmenter()
	{
		return new UkrainianSentenceSegmenter();
	}

	public IMorphologyAnalyzer CreateMorphologyAnalyzer()
	{
		var disambiguationRules = new IDisambiguationRule[]
		{
			new PrepositionCaseRule()
		};

		return new UkrainianMorphologyAnalyzer(_lexicon, disambiguationRules);
	}

	public ISpellChecker CreateSpellChecker()
	{
		return new UkrainianSpellChecker(_fuzzyMatcher, _lexicon);
	}

	public IEnumerable<IRule> GetRules()
	{
		yield return new MixedAlphabetRule();
		//yield return new AdjectiveNounAgreementRule();
	}
}