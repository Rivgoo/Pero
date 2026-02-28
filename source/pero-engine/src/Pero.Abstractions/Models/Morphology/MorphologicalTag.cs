namespace Pero.Abstractions.Models.Morphology;

/// <summary>
/// Abstract base class for language-specific morphological tags.
/// Using an abstract class instead of an interface eliminates struct Boxing 
/// and provides blazingly fast reference equality checks.
/// </summary>
public abstract class MorphologicalTag
{
}