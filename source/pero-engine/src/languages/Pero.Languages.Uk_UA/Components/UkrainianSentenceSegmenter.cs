using Pero.Abstractions.Models;
using Pero.Kernel.Components;
using Pero.Languages.Uk_UA.Configuration;

namespace Pero.Languages.Uk_UA.Components;

public class UkrainianSentenceSegmenter : BaseSentenceSegmenter
{
	public UkrainianSentenceSegmenter() : base(
		UkrainianSegmenterConstants.Terminators,
		UkrainianSegmenterConstants.ClosingQuotes)
	{
	}

	protected override bool ShouldBreakSentence(List<Token> context, int currentIndex)
	{
		var terminator = context[currentIndex].Text;
		var (prev, prevIndex) = GetPreviousSignificantWithIndex(context, currentIndex);
		var next = GetNextSignificant(context, currentIndex);

		if (next == null) return true;
		if (IsInternalPunctuation(next)) return false;

		if (IsDirectSpeechAttribution(context, currentIndex)) return false;

		if (terminator.Contains('!') || terminator.Contains('?')) return true;

		if (terminator.Contains(".."))
		{
			if (IsLowerCase(next)) return false;
			if (prev == null) return false;
			return true;
		}

		if (terminator == ".")
		{
			var (effectivePrev, effectivePrevIndex) = ResolveEffectivePreviousToken(context, prev, prevIndex);
			if (effectivePrev == null) return true;

			if (IsLowerCase(next)) return false;

			var textLower = effectivePrev.Text.ToLowerInvariant();

			var (wordBefore, _) = GetPreviousSignificantWithIndex(context, effectivePrevIndex);
			bool hasNumberBefore = IsNumber(wordBefore);
			bool hasSymbolBefore = wordBefore != null && (wordBefore.Type == TokenType.Symbol || wordBefore.Text == "/" || wordBefore.Text == "-" || wordBefore.Text == "°");

			if (IsCompositeAbbreviation(context, effectivePrevIndex, textLower))
			{
				if (IsCapitalized(next)) return true;
				return false;
			}

			bool isStructural = UkrainianSegmenterConstants.StructuralAbbreviations.Contains(textLower);
			bool isTitle = UkrainianSegmenterConstants.TitleAbbreviations.Contains(textLower);
			bool isUnit = UkrainianSegmenterConstants.UnitAbbreviations.Contains(textLower);
			bool isInitial = IsInitial(effectivePrev);

			if (isInitial)
			{
				if (hasSymbolBefore || hasNumberBefore)
				{
					isInitial = false;
					isUnit = true;
				}
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
				if (IsInitial(next)) return false;

				if (IsCapitalized(next))
				{
					var actualNext = currentIndex + 1 < context.Count ? context[currentIndex + 1] : null;
					if (actualNext?.Type != TokenType.Whitespace) return false;

					if (IsEndOfSentenceInitials(context, effectivePrevIndex)) return true;
					return false;
				}
			}

			if (isStructural || isTitle)
			{
				var realNext = GetNextSignificantSkippingQuotes(context, currentIndex);

				if (realNext != null)
				{
					if (IsInternalPunctuation(realNext)) return false;

					if (IsLowerCase(realNext)) return false;
				}

				if (IsCapitalized(next) || IsNumber(next)) return false;
			}

			if (isUnit)
			{
				if (IsDateSentenceStarter(context, effectivePrevIndex)) return false;
				if (IsCapitalized(next)) return true;
				if (IsNumber(next)) return false;
			}

			if (IsNumber(prev))
			{
				if (IsNumber(next)) return false;
				if (IsCapitalized(next)) return true;
			}
		}

		return true;
	}

	private bool IsCompositeAbbreviation(List<Token> context, int lastPartIndex, string lastPartText)
	{
		var (prevDot, dotIndex) = GetPreviousSignificantWithIndex(context, lastPartIndex);
		if (prevDot?.Text == ".")
		{
			var (firstPart, _) = GetPreviousSignificantWithIndex(context, dotIndex);
			if (firstPart != null && firstPart.Type == TokenType.Word)
			{
				string compositeKey = $"{firstPart.Text.ToLowerInvariant()}.{lastPartText}";
				if (UkrainianSegmenterConstants.StructuralAbbreviations.Contains(compositeKey))
				{
					return true;
				}
			}
		}
		return false;
	}

	private Token? GetNextSignificantSkippingQuotes(List<Token> context, int currentIndex)
	{
		for (int i = currentIndex + 1; i < context.Count; i++)
		{
			var t = context[i];
			if (t.Type == TokenType.Whitespace) continue;
			if (UkrainianSegmenterConstants.ClosingQuotes.Contains(t.Text)) continue;

			return t;
		}
		return null;
	}

	private (Token? Token, int Index) ResolveEffectivePreviousToken(List<Token> context, Token? prev, int prevIndex)
	{
		if (prev != null && UkrainianSegmenterConstants.ClosingQuotes.Contains(prev.Text))
		{
			return GetPreviousSignificantWithIndex(context, prevIndex);
		}
		return (prev, prevIndex);
	}

	private bool IsDirectSpeechAttribution(List<Token> context, int terminatorIndex)
	{
		int i = terminatorIndex + 1;
		while (i < context.Count && (UkrainianSegmenterConstants.ClosingQuotes.Contains(context[i].Text) || context[i].Type == TokenType.Whitespace))
		{
			i++;
		}
		if (i >= context.Count) return false;

		var token = context[i];
		if (token.Text == "—" || token.Text == "–" || token.Text == "-")
		{
			var nextAfterDash = GetNextSignificant(context, i);
			if (nextAfterDash == null) return false;

			if (IsLowerCase(nextAfterDash)) return true;

			if (UkrainianSegmenterConstants.StructuralAbbreviations.Contains(nextAfterDash.Text.ToLowerInvariant()))
			{
				return true;
			}
		}
		return false;
	}

	private bool IsEndOfSentenceInitials(List<Token> context, int lastInitialIndex)
	{
		var (prevDot, pdIndex) = GetPreviousSignificantWithIndex(context, lastInitialIndex);
		if (prevDot?.Text == ".")
		{
			var (firstInitial, fiIndex) = GetPreviousSignificantWithIndex(context, pdIndex);
			if (IsInitial(firstInitial))
			{
				var (surname, sIndex) = GetPreviousSignificantWithIndex(context, fiIndex);
				if (surname != null && IsCapitalized(surname) && !IsInitial(surname))
				{
					var (beforeSurname, _) = GetPreviousSignificantWithIndex(context, sIndex);
					bool isFirstWord = beforeSurname == null || IsPotentialTerminator(beforeSurname);
					if (!isFirstWord) return true;
				}
			}
		}
		return false;
	}

	private bool IsDateSentenceStarter(List<Token> context, int abbrIndex)
	{
		var (valToken, valIndex) = GetPreviousSignificantWithIndex(context, abbrIndex);
		if (valToken == null) return false;

		bool isNumberOrRoman = valToken.Type == TokenType.Number ||
							  (valToken.Type == TokenType.Word && IsCapitalized(valToken));
		if (!isNumberOrRoman) return false;

		var (prepToken, prepIndex) = GetPreviousSignificantWithIndex(context, valIndex);
		if (prepToken == null || prepToken.Type != TokenType.Word) return false;

		if (!IsCapitalized(prepToken)) return false;

		var (beforePrep, _) = GetPreviousSignificantWithIndex(context, prepIndex);
		if (beforePrep == null || IsPotentialTerminator(beforePrep))
		{
			return true;
		}

		return false;
	}
}