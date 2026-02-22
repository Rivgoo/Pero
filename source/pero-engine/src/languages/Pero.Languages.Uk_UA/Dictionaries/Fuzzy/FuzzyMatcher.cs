using System.Buffers.Binary;
using Pero.Languages.Uk_UA.Dictionaries.Models;

namespace Pero.Languages.Uk_UA.Dictionaries.Fuzzy;

/// <summary>
/// Performs fuzzy searching against the compiled binary FST dictionary using a Levenshtein Automaton.
/// </summary>
public class FuzzyMatcher
{
	private readonly byte[] _fstData;
	private readonly FlatMorphologyRule[] _rules;
	private readonly char[] _suffixPool;

	// Maximum candidates to return to prevent overwhelming the UI or processing time
	private const int MaxResults = 5;

	public FuzzyMatcher(CompiledDictionary dictionary)
	{
		// To access internal binary data, CompiledDictionary must expose it,
		// or FuzzyMatcher should be constructed inside it.
		// For this architecture, we assume CompiledDictionary exposes these via properties or methods.
		_fstData = dictionary.FstData;
		_rules = dictionary.Rules;
		_suffixPool = dictionary.SuffixPool;
	}

	/// <summary>
	/// Finds spellchecking candidates for the given word within the specified maximum edit distance.
	/// </summary>
	public IReadOnlyList<CorrectionCandidate> Suggest(string targetWord, int maxDistance = 2)
	{
		if (string.IsNullOrWhiteSpace(targetWord) || _fstData.Length == 0)
			return Array.Empty<CorrectionCandidate>();

		var results = new List<CorrectionCandidate>();

		// Initial state buffer allocation (stackalloc for zero heap allocations during traversal)
		Span<int> initialBuffer = stackalloc int[targetWord.Length + 1];
		var initialState = LevenshteinState.CreateInitial(targetWord.Length, initialBuffer);

		// Current word path allocation
		Span<char> currentWord = stackalloc char[128]; // Max word length assumption

		// Start DFS traversal from root node (offset 0)
		TraverseFst(0, targetWord, maxDistance, initialState, currentWord, 0, results);

		// Sort and trim results
		results.Sort();
		if (results.Count > MaxResults)
		{
			results.RemoveRange(MaxResults, results.Count - MaxResults);
		}

		return results;
	}

	private void TraverseFst(
		uint currentOffset,
		ReadOnlySpan<char> targetWord,
		int maxDistance,
		LevenshteinState currentState,
		Span<char> currentWord,
		int wordLength,
		List<CorrectionCandidate> results)
	{
		// Pruning: if this branch cannot possibly yield a match within maxDistance, stop.
		if (!currentState.CanMatch(maxDistance))
			return;

		byte flags = _fstData[(int)currentOffset];
		byte arcCount = _fstData[(int)currentOffset + 1];

		bool isFinal = (flags & 0x01) != 0;
		bool hasPayload = (flags & 0x02) != 0;

		int ptr = (int)currentOffset + 2;

		// 1. Process Final State (Is it a match?)
		if (isFinal && hasPayload)
		{
			int finalDistance = currentState.GetFinalDistance();
			if (finalDistance <= maxDistance)
			{
				ExtractPayloadAndAddCandidate(ptr, currentWord.Slice(0, wordLength), finalDistance, results);
			}

			// Skip over payload to reach arcs
			ptr += 1; // Frequency byte
			ushort ruleCount = BinaryPrimitives.ReadUInt16LittleEndian(_fstData.AsSpan(ptr));
			ptr += 2 + (ruleCount * 2);
		}

		// 2. Traverse Arcs
		for (int i = 0; i < arcCount; i++)
		{
			char transitionChar = (char)BinaryPrimitives.ReadUInt16LittleEndian(_fstData.AsSpan(ptr));
			uint nextOffset = BinaryPrimitives.ReadUInt32LittleEndian(_fstData.AsSpan(ptr + 2));
			ptr += 6;

			// Advance the state machine
			Span<int> nextBuffer = stackalloc int[targetWord.Length + 1];
			var nextState = currentState.Step(transitionChar, targetWord, nextBuffer);

			// Append character to the path
			currentWord[wordLength] = transitionChar;

			TraverseFst(nextOffset, targetWord, maxDistance, nextState, currentWord, wordLength + 1, results);
		}
	}

	private void ExtractPayloadAndAddCandidate(int payloadPtr, ReadOnlySpan<char> form, int distance, List<CorrectionCandidate> results)
	{
		byte frequency = _fstData[payloadPtr];
		payloadPtr += 1;

		ushort ruleCount = BinaryPrimitives.ReadUInt16LittleEndian(_fstData.AsSpan(payloadPtr));
		payloadPtr += 2;

		// For spellchecking, we reconstruct the lemma. 
		// If a form has multiple rules (homonyms), they usually reconstruct to the same lemma or we pick the first valid one.
		if (ruleCount > 0)
		{
			ushort ruleId = BinaryPrimitives.ReadUInt16LittleEndian(_fstData.AsSpan(payloadPtr));
			var rule = _rules[ruleId];

			string lemma = ApplyRule(form, rule);

			// Avoid duplicates if another homonym already added this lemma
			if (!results.Any(c => c.Word == lemma))
			{
				results.Add(new CorrectionCandidate(lemma, distance, frequency));
			}
		}
	}

	private string ApplyRule(ReadOnlySpan<char> form, FlatMorphologyRule rule)
	{
		if (rule.CutLength > form.Length) return form.ToString();

		int prefixLen = form.Length - rule.CutLength;
		int totalLen = prefixLen + rule.SuffixLength;

		var state = (Form: form.ToString(), Rule: rule, Pool: _suffixPool, PrefixLen: prefixLen);

		return string.Create(totalLen, state, static (span, st) =>
		{
			st.Form.AsSpan(0, st.PrefixLen).CopyTo(span);
			var suffixSpan = new ReadOnlySpan<char>(st.Pool, (int)st.Rule.SuffixOffset, st.Rule.SuffixLength);
			suffixSpan.CopyTo(span.Slice(st.PrefixLen));
		});
	}
}