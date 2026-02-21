using Pero.Abstractions.Models;

namespace Pero.Kernel.Components;

/// <summary>
/// A factory providing a RegexPreTokenizer pre-configured with standard patterns
/// for common technical entities like URLs, emails, and code snippets.
/// </summary>
public class StandardPreTokenizer : RegexPreTokenizer
{
	private static readonly IReadOnlyDictionary<string, string> _commonPatterns = new Dictionary<string, string>
	{
		{ nameof(FragmentType.CodeSnippet), @"`[^`\n\r]+?`|```[\s\S]+?```" },
		{ nameof(FragmentType.Url), @"https?:\/\/(www\.)?[-a-zA-Z0-9@:%._\+~#=]{1,256}\.[a-zA-Z0-9()]{1,6}\b([-a-zA-Z0-9()@:%_\+.~#?&//=]*)" },
		{ nameof(FragmentType.Email), @"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}" },
		{ nameof(FragmentType.Mention), @"[@#][\w\d_]+" }
	};

	public StandardPreTokenizer() : base(_commonPatterns)
	{
	}
}