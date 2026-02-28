using Pero.Abstractions.Models;

namespace Pero.Kernel.Configuration;

public class PreTokenizerConfig
{
	public IReadOnlyDictionary<FragmentType, string> TechnicalPatterns { get; }
	public IReadOnlyList<string> CodePatterns { get; }

	public PreTokenizerConfig(
		IReadOnlyDictionary<FragmentType, string> technicalPatterns,
		IReadOnlyList<string> codePatterns)
	{
		TechnicalPatterns = technicalPatterns;
		CodePatterns = codePatterns;
	}

	public static PreTokenizerConfig CreateDefault()
	{
		var technicalPatterns = new Dictionary<FragmentType, string>
		{
			{ FragmentType.MarkdownFormat, @"(?:\*\*[^*\n\r]+\*\*|__[^_\n\r]+__|~~[^~\n\r]+~~|\*[^*\n\r]+\*|_[^_\n\r]+_)" },
			{ FragmentType.Url, @"https?://(?:www\.)?[-a-zA-Z0-9@:%._+~#=]{1,256}\.[a-zA-Z0-9()]{1,6}\b(?:[-a-zA-Z0-9()@:%_+.~#?&//=]*)" },
			{ FragmentType.Email, @"\b[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}\b" },
			{ FragmentType.CryptoWalletAddress, @"\b(?:1[a-km-zA-HJ-NP-Z1-9]{25,34}|3[a-km-zA-HJ-NP-Z1-9]{25,34}|bc1[a-zA-HJ-NP-Z0-9]{39,59}|0x[a-fA-F0-9]{40})\b" },
			{ FragmentType.Guid, @"\b[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}\b" },
			{ FragmentType.MacAddress, @"\b(?:[0-9A-Fa-f]{2}[:-]){5}(?:[0-9A-Fa-f]{2})\b" },
			{ FragmentType.IpAddress, @"\b(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\b" },
			{ FragmentType.FilePath, @"(?:[a-zA-Z]:\\[a-zA-Z0-9_\-\.\\]*[a-zA-Z0-9_\-\\]|(?<!\S)/[a-zA-Z0-9_\-\/.]*[a-zA-Z0-9_\-\/])" },
			{ FragmentType.Coordinates, @"\b[-+]?(?:[1-8]?\d(?:\.\d+)?|90(?:\.0+)?)\s*,\s*[-+]?(?:180(?:\.0+)?|(?:1[0-7]\d|[1-9]?\d)(?:\.\d+)?)\b" },
			{ FragmentType.PhoneNumber, @"(?:\+?\d{1,3}[\s-]?)?\(?\d{2,4}\)?[\s-]?\d{3}[\s-]?\d{2}[\s-]?\d{2}\b" },
			{ FragmentType.Date, @"\b(?:[0-3]?\d[\.\-/][0-1]?\d[\.\-/](?:19|20)?\d{2}|(?:19|20)\d{2}[\.\-/][0-1]?\d[\.\-/][0-3]?\d)\b" },
			{ FragmentType.Time, @"\b(?:[01]?[0-9]|2[0-3]):[0-5][0-9](?::[0-5][0-9])?(?:\s?[AaPp][Mm])?\b" },
			{ FragmentType.Currency, @"(?:[$£€¥]\s?\d{1,3}(?:[,\s]?\d{3})*(?:\.\d{2})?|\b\d{1,3}(?:[,\s]?\d{3})*(?:,\d{2})?\s?[₴₽$€£¥])" },
			{ FragmentType.Dimensions, @"\b\d{1,5}\s?[xX×]\s?\d{1,5}(?:\s?[xX×]\s?\d{1,5})?\b" },
			{ FragmentType.HexColor, @"#(?:[0-9a-fA-F]{3}){1,2}\b" },
			{ FragmentType.VersionNumber, @"\bv?\d+\.\d+\.\d+(?:-[a-zA-Z0-9.]+)?\b" },
			{ FragmentType.Mention, @"(?:[@#][a-zA-Z0-9_]+)\b" }
		};

		var codePatterns = new List<string>
		{
			@"\b(?:if|for|while|switch|catch)\s*\([^)]*\)\s*\{[^{}]*(?:\{[^{}]*\}[^{}]*)*\}(?:\s*else(?:if)?\s*(?:\([^)]*\))?\s*\{[^{}]*(?:\{[^{}]*\}[^{}]*)*\})?",
			@"\bmatch\s+[^{]+\s*\{[^{}]*(?:\{[^{}]*\}[^{}]*)*\}",
			@"\b(?:function|void|class|struct|interface|namespace|enum|func|sub)\s+[a-zA-Z_]\w*\s*(?:\([^)]*\)|:[^{]+)?\s*\{[^{}]*(?:\{[^{}]*\}[^{}]*)*\}",
			@"\bdata\s+class\s+[a-zA-Z_]\w*\s*\([^)]*\)",
			@"\bdef\s+[a-zA-Z_]\w*\([^)]*\):\s*(?:\n\s+.*)+",
			@"(?:\([^)]*\)|[a-zA-Z_]\w*)\s*=>\s*(?:\{[^{}]*\}|[^;\n]+;?)",
			@"\b(?:var|let|const|int|string|bool|double|float|char|auto|val|local)\s+[a-zA-Z_]\w*\s*(?::\s*[a-zA-Z0-9_<>\[\]?]+)?\s*(?:=\s*(?:\{[^{}]*(?:\{[^{}]*\}[^{}]*)*\}|[^;\n]+)|;)(?:\s*[a-zA-Z_]\w*\s*(?::=|=>|=)\s*[^;\n]+;)*",
			@"<([a-z][a-z0-9]*)\b[^>]*>[\s\S]*?<\/\1>",
			@"<[a-z][a-z0-9]*[^>]*\/>",
			@"(?:\.[a-zA-Z_-]+|#[a-zA-Z0-9_-]+)\s*\{[^}]+\}",
			@"\b(?:SELECT|INSERT|UPDATE|DELETE|DROP)\b[\s\S]+?;",
			@"\b(?:npm|pip|yarn|docker|git|cargo|dotnet)\s+[a-z-]+[^\n]*",
			@"\{\s*""[a-zA-Z0-9_]+""\s*:[\s\S]*?\}",
			@"\$[a-zA-Z_]\w*\s*=\s*[^;]+;",
			@"\b[a-zA-Z0-9_]+\.[a-zA-Z_]\w*\s*(?:\([^)]*\)|\{)[^;\n]*;?",
			@"\[[a-zA-Z_]\w*\s+[a-zA-Z_]\w*(?::.*?)?\];",
			@"\b[a-zA-Z_]\w*\s*(?::=|=>)\s*[^;\n]+;?",
			@"\b(?:MOV|XOR|SUB|INT|PUSH|POP)\s+[a-zA-Z0-9]+(?:,\s*[a-zA-Z0-9]+)*(?:\s*;\s*(?:MOV|XOR|SUB|INT|PUSH|POP)[^\n]*)*",
			@"\bdo\s*\|.*?\|\s*[\s\S]*?end",
			@"@[A-Z][a-zA-Z0-9_]*(?:\([^)]*\))?",
			@"(?m:^[a-zA-Z0-9_]+:\s*\n(?:^\s{2,}[a-zA-Z0-9_]+:.*(?:\n|$))+)"
		};

		return new PreTokenizerConfig(technicalPatterns, codePatterns);
	}
}