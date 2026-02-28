using System.Reflection;
using Pero.Abstractions.Constants;
using Pero.Abstractions.Contracts;
using Pero.Kernel.Components;
using Pero.Kernel.Configuration;
using Pero.Kernel.Dictionaries;
using Pero.Kernel.Fuzzy;
using Pero.Kernel.Ngrams;
using Pero.Kernel.Utils;
using Pero.Languages.Uk_UA.Components;
using Pero.Languages.Uk_UA.Components.Caching;
using Pero.Languages.Uk_UA.Components.Disambiguation;
using Pero.Languages.Uk_UA.Components.Disambiguation.Rules;
using Pero.Languages.Uk_UA.Components.Spelling.Heuristics;
using Pero.Languages.Uk_UA.Configuration;
using Pero.Languages.Uk_UA.Dictionaries.Fuzzy;
using Pero.Languages.Uk_UA.Rules.Spelling;

namespace Pero.Languages.Uk_UA;

public class UkrainianLanguageModule : ILanguageModule
{
	private const string DictionaryResourceName = "Pero.Languages.Uk_UA.Resources.uk_UA.perodic";
	private const string NgramResourceName = "Pero.Languages.Uk_UA.Resources.uk_UA.perongram";

	private readonly CompiledDictionary dictionary;
	private readonly LexiconCache lexicon;
	private readonly FuzzyMatcher fuzzyMatcher;
	private readonly VirtualSymSpell virtualSymSpell;
	private readonly NgramLanguageModel ngramLanguageModel;
	private readonly PreTokenizerConfig preTokenizerConfig;
	private readonly ISegmentationProfile segmentationProfile;
	private readonly IReadOnlyList<ISpellingHeuristic> spellingHeuristics;

	public UkrainianLanguageModule() : this(new AssemblyResourceLoader(Assembly.GetExecutingAssembly()))
	{
	}

	public UkrainianLanguageModule(IResourceLoader resourceLoader)
	{
		dictionary = LoadDictionary(resourceLoader);
		ngramLanguageModel = LoadNgramModel(resourceLoader);

		var penaltyMatrix = new UkrainianPenaltyMatrix();

		lexicon = new LexiconCache(dictionary);
		fuzzyMatcher = new FuzzyMatcher(dictionary, penaltyMatrix);
		virtualSymSpell = new VirtualSymSpell(dictionary, penaltyMatrix);

		preTokenizerConfig = PreTokenizerConfig.CreateDefault();
		segmentationProfile = new UkrainianSegmentationProfile();

		spellingHeuristics = new List<ISpellingHeuristic>
		{
			new ConsonantSimplificationHeuristic(),
			new SurzhykSuffixHeuristic(),
			new RuleOfNineHeuristic(),
			new PrefixHeuristic(),
			new VerbEndingHeuristic(),
			new ApostropheHeuristic()
		};
	}

	public string LanguageCode => LanguageCodes.Ukrainian;

	public ITextCleaner CreateTextCleaner() => new StandardTextCleaner();

	public IPreTokenizer CreatePreTokenizer()
	{
		var standard = new StandardPreTokenizer(preTokenizerConfig);
		return new HeuristicCodePreTokenizer(standard, preTokenizerConfig);
	}

	public ITokenizer CreateTokenizer() => new UkrainianTokenizer();

	public ISentenceSegmenter CreateSentenceSegmenter() => new UkrainianSentenceSegmenter(segmentationProfile);

	public IMorphologyAnalyzer CreateMorphologyAnalyzer()
	{
		var disambiguationRules = new IDisambiguationRule[]
		{
			new PrepositionCaseRule()
		};

		return new UkrainianMorphologyAnalyzer(lexicon, disambiguationRules);
	}

	public ISpellChecker CreateSpellChecker() => new UkrainianSpellChecker(
		fuzzyMatcher,
		virtualSymSpell,
		lexicon,
		ngramLanguageModel,
		spellingHeuristics);

	public IEnumerable<IRule> GetRules()
	{
		yield return new MixedAlphabetRule();
		yield return new WordBoundaryRule(dictionary);
	}

	private static CompiledDictionary LoadDictionary(IResourceLoader loader)
	{
		using var stream = loader.LoadResource(DictionaryResourceName);
		if (stream == null)
		{
			throw new FileNotFoundException($"Embedded dictionary resource '{DictionaryResourceName}' not found.");
		}

		var dict = new CompiledDictionary();
		dict.Load(stream);
		return dict;
	}

	private static NgramLanguageModel LoadNgramModel(IResourceLoader loader)
	{
		var model = new NgramLanguageModel();
		using var stream = loader.LoadResource(NgramResourceName);

		if (stream != null)
		{
			try
			{
				model.Load(stream);
			}
			catch
			{
				// Fallback to empty model to prevent crashing on initialization if resource is corrupted
			}
		}

		return model;
	}
}