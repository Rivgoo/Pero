using Pero.Abstractions.Models.Morphology;

namespace Pero.Tools.Compiler.Contracts;

public interface IMorphologyTagParser
{
	MorphologyTagset Parse(string tagString);
}