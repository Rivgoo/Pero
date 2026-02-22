using Pero.Languages.Uk_UA.Tools.Console.Constants;

namespace Pero.Languages.Uk_UA.Tools.Console.Services;

public class FrequencyQuantizer
{
	public IReadOnlyDictionary<string, byte> Quantize(IReadOnlyDictionary<string, long> rawFrequencies)
	{
		var result = new Dictionary<string, byte>(rawFrequencies.Count, StringComparer.OrdinalIgnoreCase);
		if (rawFrequencies.Count == 0) return result;

		long maxFrequency = rawFrequencies.Values.Max();
		double logMax = Math.Log(maxFrequency + 1);

		foreach (var kvp in rawFrequencies)
		{
			double logCurrent = Math.Log(kvp.Value + 1);
			byte bucket = (byte)Math.Round((logCurrent / logMax) * AppConstants.MaxFrequencyBucket);

			result[kvp.Key] = Math.Max((byte)1, bucket);
		}

		return result;
	}
}