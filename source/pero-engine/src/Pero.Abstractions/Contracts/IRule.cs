using Pero.Abstractions.Models;

namespace Pero.Abstractions.Contracts
{
	/// <summary>
	/// Defines the contract for a single analysis rule.
	/// </summary>
	public interface IRule
	{
		/// <summary>
		/// A unique, machine-readable identifier for the rule.
		/// Example: "TYPO_SPACE_BEFORE_PUNCT".
		/// </summary>
		string Id { get; }

		/// <summary>
		/// Analyzes a sentence and returns any issues found.
		/// </summary>
		/// <param name="sentence">The sentence to check.</param>
		/// <returns>A collection of found issues.</returns>
		IEnumerable<TextIssue> Check(Sentence sentence);
	}
}