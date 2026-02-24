using Pero.Abstractions.Models.Morphology;

namespace Pero.Languages.Uk_UA.Dictionaries.Models;

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
