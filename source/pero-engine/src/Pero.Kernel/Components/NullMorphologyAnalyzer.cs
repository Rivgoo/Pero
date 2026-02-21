using Pero.Abstractions.Contracts;
using Pero.Abstractions.Models;

namespace Pero.Kernel.Components;

/// <summary>
/// A default, "do-nothing" implementation of the morphology analyzer.
/// It fulfills the interface contract without performing any actions.
/// This allows for the creation of language modules that focus only on
/// non-linguistic rules (e.g., typography) without needing a full dictionary.
/// </summary>
public class NullMorphologyAnalyzer : IMorphologyAnalyzer
{
	/// <summary>
	/// Performs no action on the sentence's tokens.
	/// </summary>
	public void Enrich(Sentence sentence)
	{
		// This implementation intentionally does nothing.
	}
}