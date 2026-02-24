namespace Pero.Languages.Uk_UA.Tools.Console.Services.Typo;

public interface ITypoStrategy
{
	bool TryGenerate(string word, Random random, out string typo, out string category);
}