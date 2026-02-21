using System.Text.RegularExpressions;
using Pero.Abstractions.Contracts;
using Pero.Abstractions.Models;

namespace Pero.Kernel.Components;

public class HeuristicCodePreTokenizer : IPreTokenizer
{
	private readonly IPreTokenizer _innerTokenizer;
	private readonly Regex _codePattern;

	public HeuristicCodePreTokenizer(IPreTokenizer innerTokenizer)
	{
		_innerTokenizer = innerTokenizer;

		// Final set of refined heuristics.
		var heuristics = new[]
		{
            // C-Style Control Flow with balanced braces and optional 'else'.
            @"\b(?:if|for|while|switch|catch)\s*\([^)]*\)\s*\{[^{}]*(?:\{[^{}]*\}[^{}]*)*\}(?:\s*else(?:if)?\s*(?:\([^)]*\))?\s*\{[^{}]*(?:\{[^{}]*\}[^{}]*)*\})?",

            // Rust Match block.
            @"\bmatch\s+[^{]+\s*\{[^{}]*(?:\{[^{}]*\}[^{}]*)*\}",

            // Function/Class Definitions. Added 'data class' with optional body.
            @"\b(?:function|void|class|struct|interface|namespace|enum|func|sub)\s+[a-zA-Z_]\w*\s*(?:\([^)]*\)|:[^{]+)?\s*\{[^{}]*(?:\{[^{}]*\}[^{}]*)*\}",
			@"\bdata\s+class\s+[a-zA-Z_]\w*\s*\([^)]*\)", 
            
            // Python Def (matches indented body more reliably).
            @"\bdef\s+[a-zA-Z_]\w*\([^)]*\):\s*(?:\n\s+.*)+",

            // Arrow Functions (ensures semicolon is captured).
            @"(?:\([^)]*\)|[a-zA-Z_]\w*)\s*=>\s*(?:\{[^{}]*\}|[^;\n]+;?)",

            // Variable Declarations (consolidated).
            @"\b(?:var|let|const|int|string|bool|double|float|char|auto|val|local)\s+[a-zA-Z_]\w*\s*(?::\s*[a-zA-Z0-9_<>\[\]?]+)?\s*(?:=\s*(?:\{[^{}]*(?:\{[^{}]*\}[^{}]*)*\}|[^;\n]+)|;)(?:\s*[a-zA-Z_]\w*\s*(?::=|=>|=)\s*[^;\n]+;)*",

            // HTML/XML Tags.
            @"<([a-z][a-z0-9]*)\b[^>]*>[\s\S]*?<\/\1>",
			@"<[a-z][a-z0-9]*[^>]*\/>",

            // CSS Rules.
            @"(?:\.[a-zA-Z_-]+|#[a-zA-Z0-9_-]+)\s*\{[^}]+\}",

            // SQL Keywords.
            @"\b(?:SELECT|INSERT|UPDATE|DELETE|DROP)\b[\s\S]+?;",

            // Package Managers.
            @"\b(?:npm|pip|yarn|docker|git|cargo|dotnet)\s+[a-z-]+[^\n]*",

            // JSON Objects.
            @"\{\s*""[a-zA-Z0-9_]+""\s*:[\s\S]*?\}",

            // PHP Variables.
            @"\$[a-zA-Z_]\w*\s*=\s*[^;]+;",
            
            // Method Calls.
            @"\b[a-zA-Z0-9_]+\.[a-zA-Z_]\w*\s*(?:\([^)]*\)|\{)[^;\n]*;?",
            
            // Objective-C.
            @"\[[a-zA-Z_]\w*\s+[a-zA-Z_]\w*(?::.*?)?\];",
            
            // Pascal/Go/Lua Assignment.
            @"\b[a-zA-Z_]\w*\s*(?::=|=>)\s*[^;\n]+;?",
            
            // Assembly.
            @"\b(?:MOV|XOR|SUB|INT|PUSH|POP)\s+[a-zA-Z0-9]+(?:,\s*[a-zA-Z0-9]+)*(?:\s*;\s*(?:MOV|XOR|SUB|INT|PUSH|POP)[^\n]*)*",

            // Ruby Blocks.
            @"\bdo\s*\|.*?\|\s*[\s\S]*?end",
            
            // Annotations.
            @"@[A-Z][a-zA-Z0-9_]*(?:\([^)]*\))?",
            
            // YAML Heuristic (Multiline mode `(?m)` to anchor `^` to start of line).
            @"(?m:^[a-zA-Z0-9_]+:\s*\n(?:^\s{2,}[a-zA-Z0-9_]+:.*(?:\n|$))+)"
		};

		var combined = string.Join("|", heuristics);
		_codePattern = new Regex(combined, RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);
	}

	public IEnumerable<TextFragment> Scan(string cleanedText)
	{
		var matches = _codePattern.Matches(cleanedText);
		int lastIndex = 0;

		foreach (Match match in matches)
		{
			// Filter out empty matches from optional groups
			if (match.Length == 0) continue;

			if (match.Index > lastIndex)
			{
				var gap = cleanedText.Substring(lastIndex, match.Index - lastIndex);
				foreach (var subFragment in _innerTokenizer.Scan(gap))
				{
					yield return new TextFragment(
						subFragment.Text,
						subFragment.Type,
						subFragment.Start + lastIndex,
						subFragment.End + lastIndex
					);
				}
			}

			yield return new TextFragment(
				match.Value,
				FragmentType.CodeSnippet,
				match.Index,
				match.Index + match.Length
			);

			lastIndex = match.Index + match.Length;
		}

		if (lastIndex < cleanedText.Length)
		{
			var gap = cleanedText.Substring(lastIndex);
			foreach (var subFragment in _innerTokenizer.Scan(gap))
			{
				yield return new TextFragment(
					subFragment.Text,
					subFragment.Type,
					subFragment.Start + lastIndex,
					subFragment.End + lastIndex
				);
			}
		}
	}
}