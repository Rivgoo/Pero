namespace Pero.Tools.Compiler.Contracts;

/// <summary>
/// Defines how raw dictionary tags are parsed and serialized.
/// Plugs into the dictionary compiler to support language-specific morphology models.
/// </summary>
public interface IMorphologyCompilerPlugin
{
	/// <summary>
	/// Parses a string tag (e.g., "noun:v_naz:m") and returns a unique ID.
	/// </summary>
	ushort GetOrAddTagId(string tagString);

	/// <summary>
	/// Serializes all registered tags into a binary blob for the dictionary header.
	/// </summary>
	byte[] SerializeTags();
}