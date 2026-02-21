using FluentAssertions;
using Moq;
using Pero.Abstractions.Contracts;
using Pero.Abstractions.Models;
using Pero.Kernel.Pipeline;

namespace Pero.Tests.Kernel.Pipeline;

public class AnalysisPipelineTests
{
	private readonly Mock<ILanguageModule> _moduleMock;
	private readonly Mock<ITextCleaner> _cleanerMock;
	private readonly Mock<IPreTokenizer> _preTokenizerMock;
	private readonly Mock<ITokenizer> _tokenizerMock;
	private readonly Mock<ISentenceSegmenter> _segmenterMock;
	private readonly Mock<IMorphologyAnalyzer> _morphMock;
	private readonly Mock<IRule> _ruleMock;

	public AnalysisPipelineTests()
	{
		_moduleMock = new Mock<ILanguageModule>();
		_cleanerMock = new Mock<ITextCleaner>();
		_preTokenizerMock = new Mock<IPreTokenizer>();
		_tokenizerMock = new Mock<ITokenizer>();
		_segmenterMock = new Mock<ISentenceSegmenter>();
		_morphMock = new Mock<IMorphologyAnalyzer>();
		_ruleMock = new Mock<IRule>();

		// Wire up the factory to return our mocks
		_moduleMock.Setup(m => m.CreateTextCleaner()).Returns(_cleanerMock.Object);
		_moduleMock.Setup(m => m.CreatePreTokenizer()).Returns(_preTokenizerMock.Object);
		_moduleMock.Setup(m => m.CreateTokenizer()).Returns(_tokenizerMock.Object);
		_moduleMock.Setup(m => m.CreateSentenceSegmenter()).Returns(_segmenterMock.Object);
		_moduleMock.Setup(m => m.CreateMorphologyAnalyzer()).Returns(_morphMock.Object);
		_moduleMock.Setup(m => m.GetRules()).Returns([_ruleMock.Object]);
	}

	[Fact]
	public void Run_ShouldExecutePipelineStagesInOrder()
	{
		// Arrange
		var input = "Raw Input";
		var cleaned = "Cleaned Input";
		var fragment = new TextFragment(cleaned, FragmentType.Raw, 0, 13);
		var token = new Token("test", "test", TokenType.Word, 0, 4);
		var sentence = new Sentence([token]);

		// Setup the data flow chain
		_cleanerMock.Setup(c => c.Clean(input)).Returns(cleaned);
		_preTokenizerMock.Setup(p => p.Scan(cleaned)).Returns(new[] { fragment });
		_tokenizerMock.Setup(t => t.Tokenize(fragment)).Returns(new[] { token });
		_segmenterMock.Setup(s => s.Segment(It.IsAny<IEnumerable<Token>>())).Returns(new[] { sentence });
		_ruleMock.Setup(r => r.Check(sentence)).Returns(new List<TextIssue>());

		var pipeline = new AnalysisPipeline(_moduleMock.Object);

		// Act
		var result = pipeline.Run(input);

		// Assert - Verify call order and data passing
		_cleanerMock.Verify(c => c.Clean(input), Times.Once);
		_preTokenizerMock.Verify(p => p.Scan(cleaned), Times.Once);
		_tokenizerMock.Verify(t => t.Tokenize(fragment), Times.Once);
		_segmenterMock.Verify(s => s.Segment(It.Is<IEnumerable<Token>>(x => x.First() == token)), Times.Once);
		_morphMock.Verify(m => m.Enrich(sentence), Times.Once);
		_ruleMock.Verify(r => r.Check(sentence), Times.Once);

		result.Document.OriginalText.Should().Be(input);
		result.Document.Sentences.Should().ContainSingle().Which.Should().BeSameAs(sentence);
	}

	[Fact]
	public void Run_ShouldAggregateIssuesFromAllRules()
	{
		// Arrange
		var rule1 = new Mock<IRule>();
		rule1.Setup(r => r.Check(It.IsAny<Sentence>())).Returns(new[] { new TextIssue { RuleId = "R1" } });

		var rule2 = new Mock<IRule>();
		rule2.Setup(r => r.Check(It.IsAny<Sentence>())).Returns(new[] { new TextIssue { RuleId = "R2" } });

		_moduleMock.Setup(m => m.GetRules()).Returns(new[] { rule1.Object, rule2.Object });

		// Return valid dummy structure so the loop runs
		_segmenterMock.Setup(s => s.Segment(It.IsAny<IEnumerable<Token>>()))
			.Returns(new[] { new Sentence([]) });

		// Mock required components to avoid null refs
		_cleanerMock.Setup(c => c.Clean(It.IsAny<string>())).Returns("");
		_preTokenizerMock.Setup(p => p.Scan(It.IsAny<string>())).Returns(new List<TextFragment>());

		var pipeline = new AnalysisPipeline(_moduleMock.Object);

		// Act
		var result = pipeline.Run("test");

		// Assert
		result.Issues.Should().HaveCount(2);
		result.Issues.Should().Contain(i => i.RuleId == "R1");
		result.Issues.Should().Contain(i => i.RuleId == "R2");
	}

	[Fact]
	public void Run_ShouldHandleTechnicalFragments()
	{
		// Arrange: PreTokenizer returns a URL fragment
		var urlFragment = new TextFragment("http://site.com", FragmentType.Url, 0, 15);
		_preTokenizerMock.Setup(p => p.Scan(It.IsAny<string>())).Returns(new[] { urlFragment });

		// Cleaner just returns input
		_cleanerMock.Setup(c => c.Clean(It.IsAny<string>())).Returns("http://site.com");

		// Segmenter needs to return what it receives for verification
		IEnumerable<Token>? capturedTokens = null;
		_segmenterMock.Setup(s => s.Segment(It.IsAny<IEnumerable<Token>>()))
			.Callback<IEnumerable<Token>>(t => capturedTokens = t)
			.Returns(new List<Sentence>());

		var pipeline = new AnalysisPipeline(_moduleMock.Object);

		// Act
		pipeline.Run("http://site.com");

		// Assert: Tokenizer should NOT be called for URL fragments
		_tokenizerMock.Verify(t => t.Tokenize(It.IsAny<TextFragment>()), Times.Never);

		// The pipeline should manually wrap the fragment in a Token
		capturedTokens.Should().NotBeNull();
		var token = capturedTokens!.First();
		token.Type.Should().Be(TokenType.Url);
		token.Text.Should().Be("http://site.com");
	}
}