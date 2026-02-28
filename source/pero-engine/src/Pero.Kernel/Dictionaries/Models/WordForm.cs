using Pero.Abstractions.Models.Morphology;

namespace Pero.Kernel.Dictionaries.Models;

public readonly struct WordForm<TTag> where TTag : MorphologicalTag
{
	public string Form { get; }
	public TTag Tag { get; }

	public WordForm(string form, TTag tag)
	{
		Form = form;
		Tag = tag;
	}
}