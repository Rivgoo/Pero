namespace Pero.Kernel.Dictionaries.Models;

public class LemmaPayload : IEquatable<LemmaPayload>
{
	public ushort ParadigmId { get; }

	public LemmaPayload(ushort paradigmId)
	{
		ParadigmId = paradigmId;
	}

	public bool Equals(LemmaPayload? other)
	{
		if (other is null) return false;
		if (ReferenceEquals(this, other)) return true;
		return ParadigmId == other.ParadigmId;
	}

	public override bool Equals(object? obj) => Equals(obj as LemmaPayload);

	public override int GetHashCode() => ParadigmId.GetHashCode();
}
