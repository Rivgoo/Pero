namespace Pero.Tests.Languages.Uk_UA.Components.Spelling;

public partial class UkrainianSpellCheckerTests
{
	public class SpellingTestCase
	{
		public string Name { get; set; } = string.Empty;
		public string Input { get; set; } = string.Empty;
		public string Expected { get; set; } = string.Empty;

		public override string ToString() => $"{Name}: {Input} -> {Expected}";
	}
}