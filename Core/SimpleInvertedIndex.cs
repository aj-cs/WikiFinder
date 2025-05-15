using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SearchEngine.Analysis;
using SearchEngine.Core.Interfaces;

namespace SearchEngine.Core;

// simple inverted index implementation for benchmarking against the trie
// includes postings with positions, delta encoding, and bitset operations, but no scoring
public class SimpleInvertedIndex : IExactPrefixIndex
{
    // ----- posting list ------------------------------------------------------
    private sealed class Posting
    {
        public int DocId;
        public int Count;
        public readonly List<int> Positions;

        public Posting(int docId, int pos)
        {
            DocId = docId;
            Count = 1;
            Positions = new() { pos };
        }
    }

    // ----- fields ------------------------------------------------------------
    private readonly Dictionary<string, List<Posting>> _map = new(StringComparer.Ordinal);
    private readonly Dictionary<string, BitArray> _bitIndex = new(StringComparer.Ordinal);
    
    private bool _bitBuilt;
    private int _nextDocId;
    private bool _delta = true;

    public void SetDeltaEncoding(bool on) => _delta = on;

    // ------------------------------------------------------------------------
    public void AddDocument(int docId, IEnumerable<Token> tokens)
    {
        foreach (var t in tokens.ToList())
        {
            if (string.IsNullOrEmpty(t.Term)) continue;

            if (!_map.TryGetValue(t.Term, out var list))
                _map[t.Term] = list = new();

            if (list.Count > 0 && list[^1].DocId == docId)
            {
                int last = list[^1].Positions[^1];
                list[^1].Positions.Add(_delta ? t.Position - last : t.Position);
                list[^1].Count++;
            }
            else
            {
                list.Add(new Posting(docId, t.Position));
            }
        }
        _bitBuilt = false;
    
        if (docId >= _nextDocId)
        {
            _nextDocId = docId + 1;
        }
        BuildBits();
    }

    public void RemoveDocument(int docId, IEnumerable<Token> tokens)
    {
        // remove document from all term postings
        foreach (var t in tokens)
        {
            if (!_map.TryGetValue(t.Term, out var list)) continue;
            list.RemoveAll(p => p.DocId == docId);
            if (list.Count == 0) _map.Remove(t.Term);
        }
        _bitBuilt = false;
    }

    public bool Search(string term)
    {
        term = term.ToLowerInvariant();
        return _map.ContainsKey(term);
    }

    private void BuildBits()
    {
        if (_bitBuilt) return;
        _bitIndex.Clear();

        foreach (var (term, postings) in _map)
        {
            var bits = new BitArray(_nextDocId);
            foreach (var p in postings) bits[p.DocId] = true;
            _bitIndex[term] = bits;
        }
        _bitBuilt = true;
    }

    // Helper methods for position handling
    private static int BinarySearch(List<int> list, int value)
    {
        int left = 0, right = list.Count - 1;
        while (left <= right)
        {
            int mid = left + (right - left) / 2;
            if (list[mid] == value) return mid;
            if (list[mid] < value) left = mid + 1;
            else right = mid - 1;
        }
        return -1;
    }

    private static List<int> MergePositions(List<int> prev, List<int> cur)
    {
        var outp = new List<int>();
        // delta decoder
        if (prev.Count > 0 && prev[0] == 0) prev = Decode(prev);
        if (cur.Count > 0 && cur[0] == 0) cur = Decode(cur);

        // use binary search for each prev position
        foreach (var p in prev)
        {
            int idx = BinarySearch(cur, p + 1);
            if (idx != -1)
                outp.Add(cur[idx]);
        }
        return outp;
    }

    private static List<int> Decode(List<int> deltas)
    {
        var res = new List<int>(deltas.Count);
        int cur = 0;
        foreach (var d in deltas) { cur += d; res.Add(cur); }
        return res;
    }

    public List<int> PrefixSearchDocuments(string prefix)
    {
        prefix = prefix.ToLowerInvariant();
        var result = new HashSet<int>();
        
        foreach (var kvp in _map)
        {
            if (kvp.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                foreach (var posting in kvp.Value)
                {
                    result.Add(posting.DocId);
                }
            }
        }
        
        return result.ToList();
    }

    public List<(string word, List<int> docIds)> PrefixSearch(string prefix)
    {
        prefix = prefix.ToLowerInvariant();
        var results = new List<(string word, List<int> docIds)>();
        
        // Empty prefix should return limited set of all words
        if (string.IsNullOrWhiteSpace(prefix))
        {
            // Return top 100 words by document frequency
            return _map
                .OrderByDescending(kvp => kvp.Value.Count)
                .ThenBy(kvp => kvp.Key.Length)
                .Take(100)
                .Select(kvp => (kvp.Key, kvp.Value.Select(p => p.DocId).ToList()))
                .ToList();
        }
        
        // Get exact prefix matches
        var exactMatches = new List<(string word, List<int> docIds)>();
        foreach (var kvp in _map)
        {
            if (kvp.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                exactMatches.Add((kvp.Key, kvp.Value.Select(p => p.DocId).ToList()));
            }
        }
        
        // if no exact matches, try with progressively shorter prefixes
        string currentQuery = prefix;
        int minPrefixLength = 2; // don't go shorter than 2 characters
        
        if (exactMatches.Count == 0 && currentQuery.Length > minPrefixLength)
        {
            while (exactMatches.Count == 0 && currentQuery.Length > minPrefixLength)
            {
                // try with one character less
                currentQuery = currentQuery.Substring(0, currentQuery.Length - 1);
                
                foreach (var kvp in _map)
                {
                    if (kvp.Key.StartsWith(currentQuery, StringComparison.OrdinalIgnoreCase))
                    {
                        // calculate how much of the original query is contained in the beginning of the word
                        int matchLength = 0;
                        for (int i = 0; i < Math.Min(prefix.Length, kvp.Key.Length); i++)
                        {
                            if (char.ToLowerInvariant(prefix[i]) == char.ToLowerInvariant(kvp.Key[i]))
                            {
                                matchLength++;
                            }
                            else
                            {
                                break;
                            }
                        }
                        
                        // accept if at least 60% of the original query matches the beginning of the word
                        double matchPercentage = (double)matchLength / prefix.Length;
                        if (matchPercentage >= 0.6)
                        {
                            exactMatches.Add((kvp.Key, kvp.Value.Select(p => p.DocId).ToList()));
                        }
                    }
                }
            }
        }
        
        // Group results by whether they start with the original query, then by popularity, then by length
        return exactMatches
            .GroupBy(h => h.word.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(g => g.Key) // true group first (exact prefix matches)
            .SelectMany(g => g
                .OrderByDescending(h => h.docIds.Count) // by popularity
                .ThenBy(h => h.word.Length)             // then by length
            )
            .Distinct()
            .ToList();
    }

    public void Clear()
    {
        _map.Clear();
        _bitIndex.Clear();
        _nextDocId = 0;
        _bitBuilt = false;
    }

    public List<(int docId, int count)> BooleanSearchNaive(string expr)
    {
        BuildBits();

        BitArray? acc = null;
        string? op = null;
        
        foreach (var token in expr.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            if (token is "&&" or "||") { op = token; continue; }
            
            _bitIndex.TryGetValue(token, out var bits);
            bits ??= new BitArray(_nextDocId); // all false

            acc = acc == null
                ? (BitArray)bits.Clone()
                : op == "&&" ? acc.And(bits)
                : op == "||" ? acc.Or(bits)
                : acc;
            op = null;
        }
        if (acc == null) return new List<(int docId, int count)>();

        var docIds = new List<int>();
        for (int i = 0; i < acc.Length; i++)
            if (acc[i]) docIds.Add(i);
        if (docIds.Count == 0) return new List<(int docId, int count)>();
        
        // Return docIds with constant count=1 (no scoring)
        return docIds
            .Select(docId => (docId, 1))
            .ToList();
    }

    // Non-scored phrase search (returns documents with exact phrase matches)
    public List<(int docId, int count)> PhraseSearch(string phrase)
    {
        var words = phrase.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0) return new List<(int docId, int count)>();

        if (words.Length == 1) 
            return _map.TryGetValue(words[0], out var postings)
                ? postings.Select(p => (p.DocId, 1)).ToList()
                : new List<(int docId, int count)>();

        if (!_map.TryGetValue(words[0], out var first)) 
            return new List<(int docId, int count)>();

        var candidates = first.ToDictionary(p => p.DocId, p => new List<int>(p.Positions));
                                            
        for (int i = 1; i < words.Length; i++)
        {
            if (!_map.TryGetValue(words[i], out var next)) 
                return new List<(int docId, int count)>();
                
            var nextSet = new Dictionary<int, List<int>>();
            
            foreach (var p in next)
            {
                if (!candidates.TryGetValue(p.DocId, out var prev)) continue;
                var valid = MergePositions(prev, p.Positions);
                if (valid.Count > 0) 
                {
                    nextSet[p.DocId] = valid;
                }
            }
            candidates = nextSet;
            if (candidates.Count == 0) return new List<(int docId, int count)>();
        }
        
        return candidates.Keys
            .Select(docId => (docId, 1))
            .ToList();
    }
} 