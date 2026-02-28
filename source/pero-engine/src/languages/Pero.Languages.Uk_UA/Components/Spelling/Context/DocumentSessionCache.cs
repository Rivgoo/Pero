using Pero.Abstractions.Models;
using Pero.Languages.Uk_UA.Extensions;

namespace Pero.Languages.Uk_UA.Components.Spelling.Context;

/// <summary>
/// Scans the entire document to find correctly spelled words.
/// Gives priority to these words if they appear as spellchecking candidates later.
/// </summary>
public class DocumentSessionCache
{
	private readonly HashSet<string> _validWordsInDocument;

	public DocumentSessionCache(AnalyzedDocument document)
	{
		_validWordsInDocument = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		foreach (var sentence in document.Sentences)
		{
			foreach (var token in sentence.Tokens)
			{
				if (token.Type == TokenType.Word && !token.IsUnknown())
				{
					_validWordsInDocument.Add(token.NormalizedText);
				}
			}
		}
	}

	/// <summary>
	/// Returns a bonus distance reduction if the candidate exists elsewhere in the document.
	/// </summary>
	public float GetSessionBonus(string candidateWord)
	{
		// A massive bonus (-0.8f). If the user wrote "Шевченко" correctly in paragraph 1,
		// and "Шевчнко" in paragraph 2, the system must aggressively suggest "Шевченко".
		return _validWordsInDocument.Contains(candidateWord) ? 0.8f : 0f;
	}
}