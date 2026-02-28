using Pero.Kernel.Dictionaries.Models;

namespace Pero.Tools.Compiler.Services;

public static class RuleGenerator
{
	public static MorphologyRule Generate(string form, string lemma, ushort tagId)
	{
		int commonPrefixLength = 0;
		int minLength = Math.Min(form.Length, lemma.Length);

		for (int i = 0; i < minLength; i++)
		{
			if (form[i] != lemma[i]) break;
			commonPrefixLength++;
		}

		byte cutLength = (byte)(form.Length - commonPrefixLength);
		string addSuffix = lemma.Substring(commonPrefixLength);

		return new MorphologyRule(cutLength, addSuffix, tagId);
	}

	public static string Apply(string form, MorphologyRule rule)
	{
		if (rule.CutLength > form.Length) return form;

		var prefix = form.Substring(0, form.Length - rule.CutLength);
		return prefix + rule.AddSuffix;
	}
}