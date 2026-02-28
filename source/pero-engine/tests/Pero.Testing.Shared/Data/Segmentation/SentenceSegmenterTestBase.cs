using FluentAssertions;
using FluentAssertions.Execution;
using Pero.Abstractions.Contracts;
using Pero.Abstractions.Models;
using Pero.Kernel;
using Pero.Kernel.Components;
using Pero.Kernel.Configuration;
using Pero.Testing.Shared.Data.Segmentation;

namespace Pero.Testing.Shared.Segmentation;

public abstract class SentenceSegmenterTestBase
{
	protected abstract ITokenizer CreateTokenizer();
	protected abstract ISentenceSegmenter CreateSegmenter();

	protected virtual IPreTokenizer CreatePreTokenizer()
	{
		var config = PreTokenizerConfig.CreateDefault();
		return new HeuristicCodePreTokenizer(new StandardPreTokenizer(config), config);
	}

	protected void VerifySegmentation(SegmentationTestCase testCase, string fileName)
	{
		var cleaner = new StandardTextCleaner();
		var preTokenizer = CreatePreTokenizer();
		var tokenizer = CreateTokenizer();
		var segmenter = CreateSegmenter();

		var cleaned = cleaner.Clean(testCase.Input);
		var fragments = preTokenizer.Scan(cleaned);
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
					fragment.Text,
					fragment.Text,
					FragmentTokenMapper.Map(fragment.Type),
					fragment.Start,
					fragment.End));
			}
		}

		var sentences = segmenter.Segment(tokens).ToList();

		using (new AssertionScope())
		{
			sentences.Should().HaveSameCount(testCase.ExpectedSentences,
				because: $"File '{fileName}', Case '{testCase.Name}': Incorrect sentence count.");

			for (int i = 0; i < sentences.Count; i++)
			{
				var actualText = sentences[i].ToString();
				var expectedText = testCase.ExpectedSentences[i];

				actualText.Should().Be(expectedText,
					because: $"File '{fileName}', Case '{testCase.Name}': Mismatch in sentence #{i + 1}.");
			}
		}
	}
}