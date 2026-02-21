using System.Text;
using Pero.Abstractions.Contracts;
using Pero.Kernel.Components;
using Pero.Testing.Shared.Data.Tokenization;
using Pero.Testing.Shared.Loaders;
using Pero.Testing.Shared.Tokenization;

namespace Pero.Tests.Kernel.Components.Tokenization;

public class BaseTokenizerTests : TokenizerTestBase
{
	protected override ITokenizer CreateTokenizer() => new DefaultTokenizer();

	[Theory]
	[MemberData(nameof(GetTestCases))]
	public void Tokenize_ShouldMatchExpectedTokens(TokenizerTestCase testCase, string fileName)
	{
		VerifyTokenization(testCase, fileName);
	}

	public static IEnumerable<object[]> GetTestCases()
	{
		var suites = JsonLoader.Load<TokenizerTestSuite>("TestCases/Kernel/Tokenization");

		foreach (var (suite, fileName) in suites)
			foreach (var testCase in suite.Cases)
				yield return new object[] { testCase, fileName };
	}

	private class DefaultTokenizer : BaseTokenizer
	{
		protected override bool IsWordCharacter(string text, int cursor, Rune rune) => Rune.IsLetter(rune);
	}
}