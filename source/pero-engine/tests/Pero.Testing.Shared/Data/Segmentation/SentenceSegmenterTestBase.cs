using FluentAssertions;
using FluentAssertions.Execution;
using Pero.Abstractions.Contracts;
using Pero.Abstractions.Models;
using Pero.Kernel;
using Pero.Kernel.Components;
using Pero.Testing.Shared.Data.Segmentation;

namespace Pero.Testing.Shared.Segmentation;

public abstract class SentenceSegmenterTestBase
{
	/// <summary>
	/// Factory method to create the specific tokenizer for the language under test.
	/// </summary>
	protected abstract ITokenizer CreateTokenizer();

	/// <summary>
	/// Factory method to create the specific segmenter for the language under test.
	/// </summary>
	protected abstract ISentenceSegmenter CreateSegmenter();

	/// <summary>
	/// Optional override if a specific pre-tokenizer is needed. Defaults to Standard.
	/// </summary>
	protected virtual IPreTokenizer CreatePreTokenizer() => new HeuristicCodePreTokenizer(new StandardPreTokenizer());

	protected void VerifySegmentation(SegmentationTestCase testCase, string fileName)
	{
		// 1. Setup Mini-Pipeline
		var cleaner = new StandardTextCleaner();
		var preTokenizer = CreatePreTokenizer();
		var tokenizer = CreateTokenizer();
		var segmenter = CreateSegmenter();

		// 2. Prepare Tokens
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
				// Map technical fragments to tokens manually for the test context
				tokens.Add(new Token(
					fragment.Text,
					fragment.Text,
					FragmentTokenMapper.Map(fragment.Type),
					fragment.Start,
					fragment.End));
			}
		}

		// 3. Act
		var sentences = segmenter.Segment(tokens).ToList();

		// 4. Assert
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