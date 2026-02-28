using Pero.Languages.Uk_UA.Models.Morphology;

namespace Pero.Languages.Uk_UA.Components.Spelling.Context;

public readonly struct GrammarProfile
{
	public PartOfSpeech? ExpectedPos { get; }
	public IReadOnlyList<GrammarCase>? ExpectedCases { get; }
	public GrammarGender? ExpectedGender { get; }
	public GrammarNumber? ExpectedNumber { get; }
	public GrammarPerson? ExpectedPerson { get; }

	public bool IsEmpty => !ExpectedPos.HasValue && ExpectedCases == null && !ExpectedGender.HasValue && !ExpectedNumber.HasValue && !ExpectedPerson.HasValue;

	public GrammarProfile(
		PartOfSpeech? expectedPos = null,
		IReadOnlyList<GrammarCase>? expectedCases = null,
		GrammarGender? expectedGender = null,
		GrammarNumber? expectedNumber = null,
		GrammarPerson? expectedPerson = null)
	{
		ExpectedPos = expectedPos;
		ExpectedCases = expectedCases;
		ExpectedGender = expectedGender;
		ExpectedNumber = expectedNumber;
		ExpectedPerson = expectedPerson;
	}
}