using FluentAssertions;
using Moq;
using Pero.Abstractions.Contracts;
using Pero.Abstractions.Models;
using Pero.Abstractions.Telemetry;
using Pero.Kernel.Pipeline;

namespace Pero.Tests.Kernel.Pipeline;

public class AnalysisPipelineTests
{
	private readonly Mock<ILanguageModule> moduleMock;
	private readonly Mock<ITextCleaner> cleanerMock;
	private readonly Mock<IPreTokenizer> preTokenizerMock;
	private readonly Mock<ITokenizer> tokenizerMock;
	private readonly Mock<ISentenceSegmenter> segmenterMock;
	private readonly Mock<IMorphologyAnalyzer> morphMock;
	private readonly Mock<ISpellChecker> spellCheckerMock;
	private readonly Mock<IRule> ruleMock;

	public AnalysisPipelineTests()
	{
		moduleMock = new Mock<ILanguageModule>();
		cleanerMock = new Mock<ITextCleaner>();
		preTokenizerMock = new Mock<IPreTokenizer>();
		tokenizerMock = new Mock<ITokenizer>();
		segmenterMock = new Mock<ISentenceSegmenter>();
		morphMock = new Mock<IMorphologyAnalyzer>();
		spellCheckerMock = new Mock<ISpellChecker>();
		ruleMock = new Mock<IRule>();

		moduleMock.Setup(m => m.CreateTextCleaner()).Returns(cleanerMock.Object);
		moduleMock.Setup(m => m.CreatePreTokenizer()).Returns(preTokenizerMock.Object);
		moduleMock.Setup(m => m.CreateTokenizer()).Returns(tokenizerMock.Object);
		moduleMock.Setup(m => m.CreateSentenceSegmenter()).Returns(segmenterMock.Object);
		moduleMock.Setup(m => m.CreateMorphologyAnalyzer()).Returns(morphMock.Object);
		moduleMock.Setup(m => m.CreateSpellChecker()).Returns(spellCheckerMock.Object);
		moduleMock.Setup(m => m.GetRules()).Returns(new[] { ruleMock.Object });
	}

	[Fact]
	public void Run_ShouldExecutePipelineStagesInOrder()
	{
		var input = "Raw Input";
		var cleaned = "Cleaned Input";
		var fragment = new TextFragment(cleaned, FragmentType.Raw, 0, 13);
		var token = new Token("test", "test", TokenType.Word, 0, 4);
		var sentence = new Sentence(new[] { token });
		var document = new AnalyzedDocument(input, new[] { sentence });

		cleanerMock.Setup(c => c.Clean(input)).Returns(cleaned);
		preTokenizerMock.Setup(p => p.Scan(cleaned)).Returns(new[] { fragment });
		tokenizerMock.Setup(t => t.Tokenize(fragment)).Returns(new[] { token });
		segmenterMock.Setup(s => s.Segment(It.IsAny<IEnumerable<Token>>())).Returns(new[] { sentence });
		spellCheckerMock.Setup(s => s.Check(It.IsAny<AnalyzedDocument>(), It.IsAny<ITelemetryTracker>())).Returns(new List<TextIssue>());
		ruleMock.Setup(r => r.Check(sentence, It.IsAny<ITelemetryTracker>())).Returns(new List<TextIssue>());

		var pipeline = new AnalysisPipeline(moduleMock.Object);

		var result = pipeline.Run(input);

		cleanerMock.Verify(c => c.Clean(input), Times.Once);
		preTokenizerMock.Verify(p => p.Scan(cleaned), Times.Once);
		tokenizerMock.Verify(t => t.Tokenize(fragment), Times.Once);
		segmenterMock.Verify(s => s.Segment(It.Is<IEnumerable<Token>>(x => x.First() == token)), Times.Once);
		morphMock.Verify(m => m.Enrich(sentence), Times.Once);
		spellCheckerMock.Verify(s => s.Check(It.IsAny<AnalyzedDocument>(), It.IsAny<ITelemetryTracker>()), Times.Once);
		ruleMock.Verify(r => r.Check(sentence, It.IsAny<ITelemetryTracker>()), Times.Once);

		result.Document.OriginalText.Should().Be(input);
		result.Document.Sentences.Should().ContainSingle().Which.Should().BeSameAs(sentence);
	}
}