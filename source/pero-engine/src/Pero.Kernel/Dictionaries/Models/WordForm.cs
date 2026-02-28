using Pero.Abstractions.Models.Morphology;

namespace Pero.Kernel.Dictionaries.Models;

public readonly struct WordForm
{
	public string Form { get; }
	public MorphologyTagset Tagset { get; }

	public WordForm(string form, MorphologyTagset tagset)
	{
		Form = form;
		Tagset = tagset;
	}
}
