using System.Text;

namespace Pero.Languages.Uk_UA.Tools.Console.Services.Typo.Strategies;

public class KeyboardTypoStrategy : ITypoStrategy
{
	private static readonly Dictionary<char, char[]> Neighbors = new()
	{
		{'й', new[]{'ц','ф','і'}}, {'ц', new[]{'й','у','в','ф','і'}}, {'у', new[]{'ц','к','а','в'}},
		{'к', new[]{'у','е','п','а'}}, {'е', new[]{'к','н','р','п'}}, {'н', new[]{'е','г','о','р'}},
		{'г', new[]{'н','ш','л','о','ґ'}}, {'ш', new[]{'г','щ','д','л'}}, {'щ', new[]{'ш','з','ж','д'}},
		{'з', new[]{'щ','х','є','ж'}}, {'х', new[]{'з','ї','є'}}, {'ї', new[]{'х','\'','щ'}},
		{'ф', new[]{'й','ц','я','і'}}, {'і', new[]{'ц','у','ч','я','ф','в'}}, {'в', new[]{'у','к','с','ч','і','а'}},
		{'а', new[]{'к','е','м','с','в','п'}}, {'п', new[]{'е','н','и','м','а','р'}}, {'р', new[]{'н','г','т','и','п','о'}},
		{'о', new[]{'г','ш','ь','т','р','л'}}, {'л', new[]{'ш','щ','б','ь','о'}}, {'д', new[]{'щ','з','ю','б','л'}},
		{'ж', new[]{'з','х','ю'}}, {'є', new[]{'х','ї','ж'}}, {'я', new[]{'ф','і','ч'}}, {'ч', new[]{'я','і','в','с'}},
		{'с', new[]{'ч','в','а','м'}}, {'м', new[]{'с','а','п','и'}}, {'и', new[]{'м','п','р','т'}},
		{'т', new[]{'и','р','о','ь'}}, {'ь', new[]{'т','о','л','б'}}, {'б', new[]{'ь','л','д','ю'}}, {'ю', new[]{'б','д','ж'}}
	};

	public bool TryGenerate(string word, Random random, out string typo, out string category)
	{
		int op = random.Next(4);
		var sb = new StringBuilder(word);

		switch (op)
		{
			case 0:
				int i = random.Next(word.Length - 1);
				(sb[i], sb[i + 1]) = (sb[i + 1], sb[i]);
				category = "Transposition";
				break;
			case 1:
				sb.Remove(random.Next(1, word.Length), 1);
				category = "Omission";
				break;
			case 2:
				int idx = random.Next(word.Length);
				sb.Insert(idx, word[idx]);
				category = "Double Strike";
				break;
			case 3:
				int nIdx = random.Next(word.Length);
				if (Neighbors.TryGetValue(word[nIdx], out var adjacent))
				{
					sb[nIdx] = adjacent[random.Next(adjacent.Length)];
					category = "Keyboard Neighbor Substitution";
				}
				else
				{
					typo = string.Empty;
					category = string.Empty;
					return false;
				}
				break;
			default:
				typo = string.Empty;
				category = string.Empty;
				return false;
		}

		typo = sb.ToString();
		return true;
	}
}