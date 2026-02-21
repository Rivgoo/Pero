using Pero.Abstractions.Contracts;
using Pero.Languages.Uk_UA.Components;
using Pero.Testing.Shared.Data.Tokenization;
using Pero.Testing.Shared.Loaders;
using Pero.Testing.Shared.Tokenization;

namespace Pero.Tests.Languages.Uk_UA.Components;

public class UkrainianTokenizerTests : TokenizerTestBase
{
	protected override ITokenizer CreateTokenizer() => new UkrainianTokenizer();

	[Theory]
	[MemberData(nameof(GetTestCases))]
	public void Tokenize_ShouldHandleUkrainianSpecificRules(TokenizerTestCase testCase, string fileName)
	{
		VerifyTokenization(testCase, fileName);
	}

	public static IEnumerable<object[]> GetTestCases()
	{
		var directories = new[]
		{
			"TestCases/Tokenization/Base",
			"TestCases/Tokenization/uk-UA"
		};

		foreach (var directory in directories)
		{
			var suites = JsonLoader.Load<TokenizerTestSuite>(directory);

			foreach (var (suite, fileName) in suites)
				foreach (var testCase in suite.Cases)
					yield return new object[] { testCase, fileName };
		}
	}
}