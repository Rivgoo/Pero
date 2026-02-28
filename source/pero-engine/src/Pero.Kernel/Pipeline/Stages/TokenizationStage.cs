using Pero.Abstractions.Contracts;
using Pero.Abstractions.Models;

namespace Pero.Kernel.Pipeline.Stages;

public class TokenizationStage : IPipelineStage
{
	private readonly IPreTokenizer preTokenizer;
	private readonly ITokenizer tokenizer;

	public string Name => "Tokenization";

	public TokenizationStage(IPreTokenizer preTokenizer, ITokenizer tokenizer)
	{
		this.preTokenizer = preTokenizer;
		this.tokenizer = tokenizer;
	}

	public void Execute(AnalysisContext context)
	{
		IEnumerable<TextFragment> fragments;
		using (context.Telemetry.Measure("PreTokenization"))
		{
			fragments = preTokenizer.Scan(context.CleanedText);
		}

		foreach (var fragment in fragments)
		{
			if (fragment.Type == FragmentType.Raw)
			{
				context.Tokens.AddRange(tokenizer.Tokenize(fragment));
			}
			else
			{
				context.Tokens.Add(new Token(
					text: fragment.Text,
					normalizedText: fragment.Text,
					type: FragmentTokenMapper.Map(fragment.Type),
					start: fragment.Start,
					end: fragment.End
				));
			}
		}
	}
}