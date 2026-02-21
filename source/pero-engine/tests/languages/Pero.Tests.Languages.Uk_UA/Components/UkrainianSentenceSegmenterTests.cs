using Pero.Abstractions.Contracts;
using Pero.Languages.Uk_UA.Components;
using Pero.Testing.Shared.Data.Segmentation;
using Pero.Testing.Shared.Loaders;
using Pero.Testing.Shared.Segmentation;

namespace Pero.Tests.Languages.Uk_UA.Components;

public class UkrainianSentenceSegmenterTests : SentenceSegmenterTestBase
{
	protected override ITokenizer CreateTokenizer() => new UkrainianTokenizer();
	protected override ISentenceSegmenter CreateSegmenter() => new UkrainianSentenceSegmenter();

	[Theory]
	[MemberData(nameof(GetTestCases))]
	public void Segment_ShouldSplitSentencesCorrectly(SegmentationTestCase testCase, string fileName)
	{
		VerifySegmentation(testCase, fileName);
	}

	public static IEnumerable<object[]> GetTestCases()
	{
		var suites = JsonLoader.Load<SegmentationTestSuite>("TestCases/uk-UA/Segmentation");

		foreach (var (suite, fileName) in suites)
			foreach (var testCase in suite.Cases)
				yield return new object[] { testCase, fileName };
	}
}