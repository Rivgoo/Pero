using System.Text;
using Pero.Testing.Shared.Data;

namespace Pero.Testing.Shared.Loaders;

/// <summary>
/// Loads .ptf (Pero Test Format) files designed for exact String-to-String transformation testing.
/// Supports implicit RAW mode for readable text and explicit HEX mode for precise byte control.
/// </summary>
public static class PtfLoader
{
	private const string HeaderPrefix = "=== TEST:";
	private const string ModePrefix = "MODE:";
	private const string InputMarker = "[INPUT]";
	private const string ExpectedMarker = "[EXPECTED]";
	private const string EndMarker = "[END]";
	private const string CommentPrefix = "COMMENT:";

	/// <summary>
	/// Scans a directory for .ptf files and yields test cases compatible with xUnit [MemberData].
	/// </summary>
	/// <param name="relativePath">Path relative to the test assembly output directory.</param>
	/// <returns>An enumeration of object arrays: [InputText, ExpectedText, TestCaseName]</returns>
	public static IEnumerable<object[]> Load(string relativePath)
	{
		var directory = ResolveDirectory(relativePath);
		var files = Directory.GetFiles(directory, "*.ptf", SearchOption.AllDirectories);

		if (files.Length == 0)
		{
			throw new FileNotFoundException($"No .ptf files found in directory: {directory}");
		}

		foreach (var file in files)
		{
			foreach (var testCase in ParseFile(file))
			{
				// Format matches the parameters of the test method
				yield return new object[] { testCase.Input, testCase.Expected, testCase.Name };
			}
		}
	}

	/// <summary>
	/// Parses a single .ptf file into a sequence of TextTransformationTestCase objects.
	/// Uses a strict state machine to ensure file format integrity.
	/// </summary>
	private static IEnumerable<TextTransformationTestCase> ParseFile(string filePath)
	{
		var lines = File.ReadAllLines(filePath);
		var currentCase = new TextTransformationTestCase();
		var buffer = new StringBuilder();

		// State Machine
		string state = "IDLE";     // Valid states: IDLE, INPUT, EXPECTED
		string mode = "RAW";       // Valid modes: RAW, HEX
		string fileName = Path.GetFileName(filePath);

		for (int i = 0; i < lines.Length; i++)
		{
			var line = lines[i];
			var trimmed = line.Trim();
			var lineNumber = i + 1;

			// 1. Handle IDLE state (Outside of any test block)
			if (state == "IDLE")
			{
				// Skip empty lines and comments
				if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith(CommentPrefix))
				{
					continue;
				}
			}

			// 2. Detect Test Header
			if (line.StartsWith(HeaderPrefix))
			{
				if (state != "IDLE")
				{
					throw new InvalidDataException(
						$"File '{fileName}', Line {lineNumber}: Unexpected '{HeaderPrefix}'. " +
						$"Previous test '{currentCase.Name}' was not closed with '{EndMarker}'.");
				}

				currentCase = new TextTransformationTestCase
				{
					Name = line.Substring(HeaderPrefix.Length).Trim(),
					LineNumber = lineNumber
				};
				mode = "RAW"; // Reset to default mode for the new test
				continue;
			}

			// 3. Detect Mode Override
			if (line.StartsWith(ModePrefix))
			{
				if (string.IsNullOrEmpty(currentCase.Name))
				{
					throw new InvalidDataException(
						$"File '{fileName}', Line {lineNumber}: '{ModePrefix}' found outside of a test block.");
				}

				mode = line.Substring(ModePrefix.Length).Trim().ToUpperInvariant();

				if (mode != "RAW" && mode != "HEX")
				{
					throw new InvalidDataException(
						$"File '{fileName}', Line {lineNumber}: Unknown mode '{mode}'. Supported modes are RAW and HEX.");
				}
				continue;
			}

			// 4. Detect Input Section Start
			if (trimmed == InputMarker)
			{
				if (string.IsNullOrEmpty(currentCase.Name))
				{
					throw new InvalidDataException($"File '{fileName}', Line {lineNumber}: '{InputMarker}' found before a test header.");
				}
				state = "INPUT";
				buffer.Clear();
				continue;
			}

			// 5. Detect Expected Section Start
			if (trimmed == ExpectedMarker)
			{
				if (state != "INPUT")
				{
					throw new InvalidDataException(
						$"File '{fileName}', Line {lineNumber}: Unexpected '{ExpectedMarker}'. Expected state was 'INPUT', but current state is '{state}'.");
				}

				currentCase.Input = ProcessBuffer(buffer, mode, fileName, lineNumber);
				state = "EXPECTED";
				buffer.Clear();
				continue;
			}

			// 6. Detect End of Test Block
			if (trimmed == EndMarker)
			{
				if (state != "EXPECTED")
				{
					throw new InvalidDataException(
						$"File '{fileName}', Line {lineNumber}: Unexpected '{EndMarker}'. Expected state was 'EXPECTED', but current state is '{state}'.");
				}

				currentCase.Expected = ProcessBuffer(buffer, mode, fileName, lineNumber);

				// Yield the completed test case
				yield return currentCase;

				// Reset state machine
				state = "IDLE";
				currentCase = new TextTransformationTestCase();
				continue;
			}

			// 7. Capture Content (if inside a block)
			if (state == "INPUT" || state == "EXPECTED")
			{
				// Ignore in-block comments
				if (trimmed.StartsWith(CommentPrefix))
				{
					continue;
				}

				if (mode == "RAW")
				{
					// Preserve exact line endings for raw text
					buffer.AppendLine(line);
				}
				else // HEX
				{
					// In HEX mode, we accumulate everything on one line.
					// Spaces will be stripped during processing.
					buffer.Append(line);
				}
			}
		}

		// 8. Final Sanity Check
		if (state != "IDLE")
		{
			throw new InvalidDataException(
				$"File '{fileName}': Unexpected end of file. Test '{currentCase.Name}' (started at line {currentCase.LineNumber}) was never closed with '{EndMarker}'.");
		}
	}

	/// <summary>
	/// Converts the accumulated buffer into the final string based on the active mode.
	/// </summary>
	private static string ProcessBuffer(StringBuilder buffer, string mode, string fileName, int currentLine)
	{
		if (mode == "RAW")
		{
			var rawText = buffer.ToString();

			// StringBuilder.AppendLine adds a trailing newline that isn't part of the actual file content block.
			// We must strip exactly one trailing Environment.NewLine to maintain fidelity.
			if (rawText.EndsWith(Environment.NewLine))
			{
				return rawText.Substring(0, rawText.Length - Environment.NewLine.Length);
			}

			// Fallback for mixed line endings (e.g., file uses \n but environment is \r\n)
			if (rawText.EndsWith("\n"))
			{
				return rawText.Substring(0, rawText.Length - 1);
			}

			return rawText;
		}

		if (mode == "HEX")
		{
			// Clean the hex string of all formatting
			var hexString = buffer.ToString()
				.Replace(" ", "")
				.Replace("\t", "")
				.Replace("\r", "")
				.Replace("\n", "");

			if (string.IsNullOrEmpty(hexString))
			{
				return string.Empty;
			}

			try
			{
				byte[] bytes = Convert.FromHexString(hexString);
				return Encoding.UTF8.GetString(bytes);
			}
			catch (FormatException)
			{
				throw new InvalidDataException(
					$"File '{fileName}', Line {currentLine}: Failed to parse HEX block. Ensure the block contains only valid hexadecimal characters (0-9, A-F) and spaces.");
			}
		}

		throw new InvalidOperationException($"Unsupported mode: {mode}");
	}

	/// <summary>
	/// Resolves the absolute path to the test data directory.
	/// </summary>
	private static string ResolveDirectory(string relativePath)
	{
		var baseDir = AppContext.BaseDirectory;
		var fullPath = Path.GetFullPath(Path.Combine(baseDir, relativePath));

		if (Directory.Exists(fullPath))
			return fullPath;

		// Fallback for certain local runner configurations where BaseDirectory is deeply nested
		var candidate = Path.GetFullPath(Path.Combine(baseDir, "../../../", relativePath));
		if (Directory.Exists(candidate))
			return candidate;

		throw new DirectoryNotFoundException($"Test data directory not found at '{fullPath}' or '{candidate}'. Check your relative path and 'CopyToOutputDirectory' settings.");
	}
}