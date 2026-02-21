using FluentAssertions;
using FluentAssertions.Execution;
using Pero.Abstractions.Contracts;
using Pero.Abstractions.Models;
using Pero.Testing.Shared.Data.Tokenization;

namespace Pero.Testing.Shared.Tokenization;

public abstract class TokenizerTestBase
{
	protected abstract ITokenizer CreateTokenizer();

	protected void VerifyTokenization(TokenizerTestCase testCase, string fileName)
	{
		var tokenizer = CreateTokenizer();
		var fragment = new TextFragment(testCase.Input, FragmentType.Raw, 0, testCase.Input.Length);
		var result = tokenizer.Tokenize(fragment).ToList();

		using (new AssertionScope())
		{
			result.Should().HaveSameCount(testCase.Expected,
				because: $"File '{fileName}', Case '{testCase.Name}': Expected {testCase.Expected.Count} tokens but found {result.Count}.");

			for (int i = 0; i < result.Count; i++)
			{
				var actual = result[i];
				var expected = testCase.Expected[i];

				Enum.TryParse<TokenType>(expected.Type, true, out var expectedType)
					.Should().BeTrue($"'{expected.Type}' must be a valid TokenType enum value.");

				actual.Type.Should().Be(expectedType, $"Token {i} type mismatch in '{testCase.Name}'");
				actual.Text.Should().Be(expected.Text, $"Token {i} text mismatch in '{testCase.Name}'");
				actual.NormalizedText.Should().Be(expected.NormalizedText, $"Token {i} normalized text mismatch in '{testCase.Name}'");
				actual.Start.Should().Be(expected.Start, $"Token {i} start mismatch in '{testCase.Name}'");
				actual.End.Should().Be(expected.End, $"Token {i} end mismatch in '{testCase.Name}'");
			}
		}
	}
}