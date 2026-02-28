using System.Reflection;
using Pero.Abstractions.Contracts;

namespace Pero.Kernel.Utils;

public class AssemblyResourceLoader : IResourceLoader
{
	private readonly Assembly assembly;

	public AssemblyResourceLoader(Assembly assembly)
	{
		this.assembly = assembly;
	}

	public Stream? LoadResource(string resourceName)
	{
		return assembly.GetManifestResourceStream(resourceName);
	}
}