namespace Pero.Tools.Compiler.Services;

public class DawgBuilder<TPayload> where TPayload : IEquatable<TPayload>
{
	private readonly FstNode<TPayload> _root = new();
	private readonly List<FstNode<TPayload>> _uncheckedNodes = new();
	private readonly Dictionary<FstNode<TPayload>, FstNode<TPayload>> _minimizedNodes = new();
	private readonly Func<TPayload, TPayload, TPayload> _mergeFunc;
	private string _previousWord = string.Empty;

	public DawgBuilder(Func<TPayload, TPayload, TPayload> mergeFunc)
	{
		_uncheckedNodes.Add(_root);
		_mergeFunc = mergeFunc;
	}

	public void Insert(string word, TPayload payload)
	{
		if (string.CompareOrdinal(word, _previousWord) < 0)
			throw new InvalidOperationException("Words must be inserted in alphabetical order.");

		int commonPrefix = 0;
		int minLength = Math.Min(word.Length, _previousWord.Length);
		for (int i = 0; i < minLength; i++)
		{
			if (word[i] != _previousWord[i]) break;
			commonPrefix++;
		}

		Minimize(commonPrefix);

		for (int i = commonPrefix; i < word.Length; i++)
		{
			var newNode = new FstNode<TPayload>();
			var parent = _uncheckedNodes.Last();
			parent.Arcs[word[i]] = newNode;
			_uncheckedNodes.Add(newNode);
		}

		var lastNode = _uncheckedNodes.Last();

		if (lastNode.IsFinal && lastNode.Payload != null)
		{
			lastNode.Payload = _mergeFunc(lastNode.Payload, payload);
		}
		else
		{
			lastNode.IsFinal = true;
			lastNode.Payload = payload;
		}

		_previousWord = word;
	}

	public FstNode<TPayload> Finish()
	{
		Minimize(0);
		return _root;
	}

	private void Minimize(int downTo)
	{
		for (int i = _uncheckedNodes.Count - 1; i > downTo; i--)
		{
			var parent = _uncheckedNodes[i - 1];
			var child = _uncheckedNodes[i];
			var transitionChar = parent.Arcs.First(kvp => ReferenceEquals(kvp.Value, child)).Key;

			if (_minimizedNodes.TryGetValue(child, out var existingNode))
			{
				parent.Arcs[transitionChar] = existingNode;
			}
			else
			{
				_minimizedNodes.Add(child, child);
			}

			_uncheckedNodes.RemoveAt(i);
		}
	}
}