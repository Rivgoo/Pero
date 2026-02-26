using Pero.Abstractions.Constants;
using Pero.Abstractions.Contracts;
using Pero.Kernel.Components;
using Pero.Languages.Uk_UA.Components;
using Pero.Languages.Uk_UA.Components.Caching;
using Pero.Languages.Uk_UA.Components.Disambiguation;
using Pero.Languages.Uk_UA.Components.Disambiguation.Rules;
using Pero.Languages.Uk_UA.Dictionaries;
using Pero.Languages.Uk_UA.Dictionaries.Fuzzy;
using Pero.Languages.Uk_UA.Dictionaries.Ngrams;
using Pero.Languages.Uk_UA.Resources;
using Pero.Languages.Uk_UA.Rules.Spelling;

namespace Pero.Languages.Uk_UA;

public class UkrainianLanguageModule : ILanguageModule
{
	private readonly CompiledDictionary _dictionary;
	private readonly LexiconCache _lexicon;
	private readonly FuzzyMatcher _fuzzyMatcher;
	private readonly VirtualSymSpell _virtualSymSpell;
	private readonly NgramLanguageModel _ngramLanguageModel;

	public UkrainianLanguageModule()
	{
		_dictionary = DictionaryProvider.GetDictionary();
		_ngramLanguageModel = NgramProvider.GetModel();

		_lexicon = new LexiconCache(_dictionary);
		_fuzzyMatcher = new FuzzyMatcher(_dictionary);
		_virtualSymSpell = new VirtualSymSpell(_dictionary);
	}

	public string LanguageCode => LanguageCodes.Ukrainian;

	public ITextCleaner CreateTextCleaner() => new StandardTextCleaner();

	public IPreTokenizer CreatePreTokenizer() => new HeuristicCodePreTokenizer(new StandardPreTokenizer());

	public ITokenizer CreateTokenizer() => new UkrainianTokenizer();

	public ISentenceSegmenter CreateSentenceSegmenter() => new UkrainianSentenceSegmenter();

	public IMorphologyAnalyzer CreateMorphologyAnalyzer()
	{
		var disambiguationRules = new IDisambiguationRule[]
		{
			new PrepositionCaseRule()
		};

		return new UkrainianMorphologyAnalyzer(_lexicon, disambiguationRules);
	}

	public ISpellChecker CreateSpellChecker() => new UkrainianSpellChecker(
		_fuzzyMatcher,
		_virtualSymSpell,
		_lexicon,
		_ngramLanguageModel);

	public IEnumerable<IRule> GetRules()
	{
		yield return new MixedAlphabetRule();
		yield return new WordBoundaryRule(_dictionary);
	}
}