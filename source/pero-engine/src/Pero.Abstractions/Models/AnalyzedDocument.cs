namespace Pero.Abstractions.Models
{
	/// <summary>
	/// The root object representing the entire analyzed text.
	/// It acts as a container for sentences.
	/// </summary>
	public class AnalyzedDocument
	{
		/// <summary>
		/// The original, unmodified input text.
		/// </summary>
		public string OriginalText { get; }

		/// <summary>
		/// A list of sentences found in the document.
		/// </summary>
		public IReadOnlyList<Sentence> Sentences { get; }

		public AnalyzedDocument(string originalText, IReadOnlyList<Sentence> sentences)
		{
			OriginalText = originalText;
			Sentences = sentences;
		}
	}
}