namespace Pero.Testing.Shared.Data.Segmentation;

public class SegmentationTestSuite
{
	public string Description { get; set; } = string.Empty;
	public List<SegmentationTestCase> Cases { get; set; } = new();
}