namespace Pero.Tools.Compiler.Services;

public class FstSuffixDictionaryCompilerFacade
{
	private readonly FstSuffixDictionaryParser parser;
	private readonly FstSerializer serializer;

	public FstSuffixDictionaryCompilerFacade(FstSuffixDictionaryParser parser, FstSerializer serializer)
	{
		this.parser = parser;
		this.serializer = serializer;
	}

	public void Compile(IEnumerable<string> rawLines, Stream outputStream, IReadOnlyDictionary<string, byte>? frequencies = null)
	{
		var data = parser.Parse(rawLines, frequencies);

		if (data.ForwardRoot == null || data.LemmaRoot == null)
		{
			throw new InvalidDataException("Failed to build FST roots. Source data might be empty.");
		}

		serializer.WriteFlatBinary(
			outputStream,
			data.TagsBlob,
			data.Rules,
			data.ReverseRules,
			data.Paradigms,
			data.SuffixPool.ToString(),
			data.ForwardRoot,
			data.LemmaRoot
		);
	}
}