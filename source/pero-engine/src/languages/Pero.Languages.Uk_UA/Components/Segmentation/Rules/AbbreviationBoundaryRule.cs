using Pero.Abstractions.Contracts;
using Pero.Abstractions.Models;
using Pero.Kernel.Components.Segmentation;

namespace Pero.Languages.Uk_UA.Components.Segmentation.Rules;

public class AbbreviationBoundaryRule : ISentenceBoundaryRule
{
	public SentenceBoundaryDecision Check(IReadOnlyList<Token> context, int currentIndex, ISegmentationProfile profile)
	{
		var terminator = context[currentIndex].Text;
		if (terminator != ".") return SentenceBoundaryDecision.Pass;

		var (prev, prevIndex) = context.GetPreviousSignificantWithIndex(currentIndex);
		var (effectivePrev, effectivePrevIndex) = ResolveEffectivePreviousToken(context, prev, prevIndex, profile);
		var next = context.GetNextSignificant(currentIndex);

		if (effectivePrev == null) return SentenceBoundaryDecision.Break;
		if (next.IsLowerCase()) return SentenceBoundaryDecision.DoNotBreak;

		var textLower = effectivePrev.Text.ToLowerInvariant();
		var (wordBefore, _) = context.GetPreviousSignificantWithIndex(effectivePrevIndex);

		bool hasNumberBefore = wordBefore.IsNumber();
		bool hasSymbolBefore = wordBefore != null && (wordBefore.Type == TokenType.Symbol || wordBefore.Text == "/" || wordBefore.Text == "-" || wordBefore.Text == "°");

		if (IsCompositeAbbreviation(context, effectivePrevIndex, textLower, profile))
		{
			return next.IsCapitalized() ? SentenceBoundaryDecision.Break : SentenceBoundaryDecision.DoNotBreak;
		}

		bool isStructural = profile.StructuralAbbreviations.Contains(textLower);
		bool isTitle = profile.TitleAbbreviations.Contains(textLower);
		bool isUnit = profile.UnitAbbreviations.Contains(textLower);
		bool isInitial = effectivePrev.IsInitial();

		if (isInitial && (hasSymbolBefore || hasNumberBefore))
		{
			isInitial = false;
			isUnit = true;
		}

		if (isStructural && isUnit)
		{
			if (hasNumberBefore || hasSymbolBefore) isStructural = false;
			else isUnit = false;
		}
		else if (isStructural && hasSymbolBefore)
		{
			isStructural = false;
			isUnit = true;
		}

		if (isInitial)
		{
			if (next.IsInitial()) return SentenceBoundaryDecision.DoNotBreak;

			if (next.IsCapitalized())
			{
				var actualNext = currentIndex + 1 < context.Count ? context[currentIndex + 1] : null;
				if (actualNext?.Type != TokenType.Whitespace) return SentenceBoundaryDecision.DoNotBreak;

				if (IsEndOfSentenceInitials(context, effectivePrevIndex, profile)) return SentenceBoundaryDecision.Break;
				return SentenceBoundaryDecision.DoNotBreak;
			}
		}

		if (isStructural || isTitle)
		{
			var realNext = context.GetNextSignificantSkippingQuotes(currentIndex, profile);

			if (realNext.IsInternalPunctuation() || realNext.IsLowerCase()) return SentenceBoundaryDecision.DoNotBreak;
			if (next.IsCapitalized() || next.IsNumber()) return SentenceBoundaryDecision.DoNotBreak;
		}

		if (isUnit)
		{
			if (IsDateSentenceStarter(context, effectivePrevIndex, profile)) return SentenceBoundaryDecision.DoNotBreak;
			if (next.IsCapitalized()) return SentenceBoundaryDecision.Break;
			if (next.IsNumber()) return SentenceBoundaryDecision.DoNotBreak;
		}

		if (prev.IsNumber())
		{
			if (next.IsNumber()) return SentenceBoundaryDecision.DoNotBreak;
			if (next.IsCapitalized()) return SentenceBoundaryDecision.Break;
		}

		return SentenceBoundaryDecision.Pass;
	}

	private (Token? Token, int Index) ResolveEffectivePreviousToken(IReadOnlyList<Token> context, Token? prev, int prevIndex, ISegmentationProfile profile)
	{
		if (prev != null && profile.ClosingQuotes.Contains(prev.Text))
		{
			return context.GetPreviousSignificantWithIndex(prevIndex);
		}
		return (prev, prevIndex);
	}

	private bool IsCompositeAbbreviation(IReadOnlyList<Token> context, int lastPartIndex, string lastPartText, ISegmentationProfile profile)
	{
		var (prevDot, dotIndex) = context.GetPreviousSignificantWithIndex(lastPartIndex);
		if (prevDot?.Text == ".")
		{
			var (firstPart, _) = context.GetPreviousSignificantWithIndex(dotIndex);
			if (firstPart != null && firstPart.Type == TokenType.Word)
			{
				string compositeKey = $"{firstPart.Text.ToLowerInvariant()}.{lastPartText}";
				if (profile.StructuralAbbreviations.Contains(compositeKey)) return true;
			}
		}
		return false;
	}

	private bool IsEndOfSentenceInitials(IReadOnlyList<Token> context, int lastInitialIndex, ISegmentationProfile profile)
	{
		var (prevDot, pdIndex) = context.GetPreviousSignificantWithIndex(lastInitialIndex);
		if (prevDot?.Text == ".")
		{
			var (firstInitial, fiIndex) = context.GetPreviousSignificantWithIndex(pdIndex);
			if (firstInitial.IsInitial())
			{
				var (surname, sIndex) = context.GetPreviousSignificantWithIndex(fiIndex);
				if (surname.IsCapitalized() && !surname.IsInitial())
				{
					var (beforeSurname, _) = context.GetPreviousSignificantWithIndex(sIndex);
					bool isFirstWord = beforeSurname == null || (beforeSurname.Type == TokenType.Punctuation && profile.Terminators.Contains(beforeSurname.Text));
					if (!isFirstWord) return true;
				}
			}
		}
		return false;
	}

	private bool IsDateSentenceStarter(IReadOnlyList<Token> context, int abbrIndex, ISegmentationProfile profile)
	{
		var (valToken, valIndex) = context.GetPreviousSignificantWithIndex(abbrIndex);
		if (valToken == null) return false;

		bool isNumberOrRoman = valToken.Type == TokenType.Number || valToken.IsCapitalized();
		if (!isNumberOrRoman) return false;

		var (prepToken, prepIndex) = context.GetPreviousSignificantWithIndex(valIndex);
		if (prepToken == null || prepToken.Type != TokenType.Word) return false;

		if (!prepToken.IsCapitalized()) return false;

		var (beforePrep, _) = context.GetPreviousSignificantWithIndex(prepIndex);
		if (beforePrep == null || (beforePrep.Type == TokenType.Punctuation && profile.Terminators.Contains(beforePrep.Text)))
		{
			return true;
		}

		return false;
	}
}