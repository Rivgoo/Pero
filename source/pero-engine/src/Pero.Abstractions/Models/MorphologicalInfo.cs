namespace Pero.Abstractions.Models
{
	/// <summary>
	/// A container for morphological annotations of a token.
	/// Provides common linguistic properties and allows for language-specific extensions.
	/// </summary>
	public class MorphologicalInfo
	{
		/// <summary>
		/// The base or dictionary form of a word. Example: "went" -> "go".
		/// </summary>
		public string Lemma { get; }

		/// <summary>
		/// The primary grammatical category of the word.
		/// </summary>
		public PartOfSpeech PartOfSpeech { get; }

		/// <summary>
		/// A collection of language-specific features.
		/// Example: {"Case": "Genitive", "Gender": "Masculine"}.
		/// This design allows extension without changing the core model.
		/// </summary>
		public IReadOnlyDictionary<string, string> Tags { get; }

		public MorphologicalInfo(string lemma, PartOfSpeech pos, IReadOnlyDictionary<string, string> tags)
		{
			Lemma = lemma;
			PartOfSpeech = pos;
			Tags = tags;
		}
	}
}