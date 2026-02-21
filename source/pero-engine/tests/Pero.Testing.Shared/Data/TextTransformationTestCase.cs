namespace Pero.Testing.Shared.Data;

/// <summary>
/// Represents a test case for "String Input -> String Output" scenarios.
/// Used primarily for Cleaner and Normalizer tests.
/// </summary>
public class TextTransformationTestCase
{
	public string Name { get; set; } = string.Empty;
	public string Input { get; set; } = string.Empty;
	public string Expected { get; set; } = string.Empty;
	public int LineNumber { get; set; }
}