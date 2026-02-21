namespace Pero.Testing.Shared.Data.Segmentation;

public class SegmentationTestCase
{
	public string Name { get; set; } = string.Empty;
	public string Input { get; set; } = string.Empty;
	public List<string> ExpectedSentences { get; set; } = new();
}