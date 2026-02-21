namespace Pero.Testing.Shared.Data.Tokenization;

public class TokenizerTestCase
{
	public string Name { get; set; } = string.Empty;
	public string Input { get; set; } = string.Empty;
	public List<ExpectedToken> Expected { get; set; } = new();
}