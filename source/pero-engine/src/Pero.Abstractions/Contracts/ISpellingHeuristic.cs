namespace Pero.Abstractions.Contracts;

public interface ISpellingHeuristic
{
	IEnumerable<string> Generate(string word);
}