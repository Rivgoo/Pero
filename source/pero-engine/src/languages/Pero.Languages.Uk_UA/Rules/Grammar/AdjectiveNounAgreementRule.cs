using Pero.Abstractions.Models;
using Pero.Abstractions.Models.Morphology;
using Pero.Kernel.Rules;
using Pero.Kernel.Utils;

namespace Pero.Languages.Uk_UA.Rules.Grammar;

/// <summary>
/// Example rule demonstrating the fluent morphology API.
/// Checks if an adjective matches the case and gender of the following noun.
/// </summary>
public class AdjectiveNounAgreementRule : BaseGrammarRule
{
	public override string Id => "UK_UA_GRAMMAR_ADJ_NOUN_AGREEMENT";
	public override IssueCategory Category => IssueCategory.Grammar;
	public override IssueSeverity Severity => IssueSeverity.Warning;

	protected override IEnumerable<TextIssue> Analyze(Sentence sentence)
	{
		for (int i = 0; i < sentence.Tokens.Count - 1; i++)
		{
			var current = sentence.Tokens[i];
			var next = sentence.GetNextSignificantToken(current);

			if (next == null) continue;

			if (current.Is(PartOfSpeech.Adjective) && next.Is(PartOfSpeech.Noun))
			{
				// Only check if both tokens were successfully disambiguated
				if (current.Morph == null || next.Morph == null) continue;

				bool caseMismatch = current.Morph.Tagset.Case != next.Morph.Tagset.Case;
				bool genderMismatch = current.Morph.Tagset.Gender != next.Morph.Tagset.Gender && current.Morph.Tagset.Number == GrammarNumber.Singular; // Gender is merged in Plural

				if (caseMismatch || genderMismatch)
				{
					var args = new Dictionary<string, string>
					{
						{ "adj", current.Text },
						{ "noun", next.Text }
					};

					yield return IssueFactory.CreateSpanning(
						new[] { current, next },
						Id,
						Category,
						sentence.ToString(),
						null,
						args
					);
				}
			}
		}
	}
}