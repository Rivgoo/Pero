namespace Pero.Abstractions.Contracts;

public interface IResourceLoader
{
	Stream? LoadResource(string resourceName);
}