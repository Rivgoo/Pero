using Pero.Languages.Uk_UA.Dictionaries.Models;

namespace Pero.Languages.Uk_UA.Dictionaries.Builder;

/// <summary>
/// Constructs a minimal FST by merging identical suffixes incrementally.
/// Input words MUST be inserted in strictly alphabetical order.
/// </summary>
public class DawgBuilder
{
	private readonly FstNode _root = new();
	private readonly List<FstNode> _uncheckedNodes = new();
	private readonly Dictionary<FstNode, FstNode> _minimizedNodes = new();
	private string _previousWord = string.Empty;

	public DawgBuilder()
	{
		_uncheckedNodes.Add(_root);
	}

	public void Insert(string word, FstPayload payload)
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
			var newNode = new FstNode();
			var parent = _uncheckedNodes.Last();
			parent.Arcs[word[i]] = newNode;
			_uncheckedNodes.Add(newNode);
		}

		var lastNode = _uncheckedNodes.Last();

		if (lastNode.IsFinal && lastNode.Payload != null)
		{
			// Word already exists (homonym). Merge payloads (Rule IDs).
			var mergedRules = lastNode.Payload.RuleIds.Concat(payload.RuleIds).Distinct().ToArray();
			lastNode.Payload = new FstPayload(Math.Max(lastNode.Payload.Frequency, payload.Frequency), mergedRules);
		}
		else
		{
			lastNode.IsFinal = true;
			lastNode.Payload = payload;
		}

		_previousWord = word;
	}

	public FstNode Finish()
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