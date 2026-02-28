namespace Pero.Tools.Compiler.Services;

public class DictionaryCompilerFacade
{
	private readonly DictionaryParser parser;
	private readonly FstSerializer serializer;

	public DictionaryCompilerFacade(DictionaryParser parser, FstSerializer serializer)
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
			data.Tagsets,
			data.Rules,
			data.ReverseRules,
			data.Paradigms,
			data.SuffixPool.ToString(),
			data.ForwardRoot,
			data.LemmaRoot
		);
	}
}