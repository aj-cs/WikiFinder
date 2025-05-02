using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SearchEngine.Analysis;
using SearchEngine.Core.Interfaces;

public sealed class InvertedIndex : IFullTextIndex
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
    private readonly Dictionary<int, string>           _docTitles = new(); // only needed for debug
    private readonly Dictionary<string, BitArray>       _bitIndex = new(StringComparer.Ordinal);
    private bool _bitBuilt;
    private int  _nextDocId;

    private bool _delta = true;            
    public void SetDeltaEncoding(bool on) => _delta = on;

    // ------------------------------------------------------------------------
    public void AddDocument(int docId, IEnumerable<Token> tokens)
    {
        foreach (var t in tokens)
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
        
        // Update next document ID for bit array sizing
        if (docId >= _nextDocId)
        {
            _nextDocId = docId + 1;
        }
    }

    public void RemoveDocument(int docId, IEnumerable<Token> tokens)
    {
        foreach (var t in tokens)
        {
            if (!_map.TryGetValue(t.Term, out var list)) continue;
            list.RemoveAll(p => p.DocId == docId);
            if (list.Count == 0) _map.Remove(t.Term);
        }
        _bitBuilt = false;
    }

    public void Clear()
    {
        _map.Clear();           // remove all postings
        _bitIndex.Clear();      // remove all bit-vectors
        _docTitles.Clear();     // forget any stored titles/debug info
        _nextDocId = 0;         // reset doc-id counter
        _bitBuilt = false;      // mark bits as needing rebuild
    }
    

    // ------------------------------------------------------------------------
    public List<int> ExactSearch(string term)
        => _map.TryGetValue(term, out var p)
           ? p.Select(x => x.DocId).ToList()
           : new List<int>();

    public List<int> PhraseSearch(string phrase)
    {
        var words = phrase.Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0) return new List<int>();

        if (words.Length == 1) return ExactSearch(words[0]);

        if (!_map.TryGetValue(words[0], out var first)) return new List<int>();

        var candidates = first.ToDictionary(p => p.DocId,
                                            p => new List<int>(p.Positions));
        for (int i = 1; i < words.Length; i++)
        {
            if (!_map.TryGetValue(words[i], out var next)) return new List<int>();
            var nextSet = new Dictionary<int,List<int>>();
            foreach (var p in next)
            {
                if (!candidates.TryGetValue(p.DocId, out var prev)) continue;
                var valid = MergePositions(prev, p.Positions);
                if (valid.Count > 0) nextSet[p.DocId] = valid;
            }
            candidates = nextSet;
            if (candidates.Count == 0) return new List<int>();
        }
        return candidates.Keys.ToList();
    }

    public List<int> BooleanSearch(string expr)
    {
        BuildBits();

        BitArray? acc = null;
        string? op   = null;
        foreach (var token in expr.Split(' ', System.StringSplitOptions.RemoveEmptyEntries))
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
        if (acc == null) return new List<int>();

        var hits = new List<int>();
        for (int i = 0; i < acc.Length; i++)
            if (acc[i]) hits.Add(i);
        return hits;
    }
    // ------------------------------------------------------------------------
    public List<(int docId, double score)> RankedSearch(string termWithHash)
    {
        string term = termWithHash.TrimEnd('#');
        if (!_map.TryGetValue(term, out var list)) return new List<(int, double)>();
        return list
              .OrderByDescending(p => p.Count)
              .Select(p => (p.DocId, (double)p.Count))
              .ToList();
    }
    // ------------------------------------------------------------------------
    private static List<int> MergePositions(List<int> prev, List<int> cur)
    {
        var outp = new List<int>();
        // delta-decode
        if (prev.Count > 0 && prev[0] == 0) prev = Decode(prev);
        if (cur.Count  > 0 && cur [0] == 0) cur  = Decode(cur);

        int i = 0, j = 0;
        while (i < prev.Count && j < cur.Count)
        {
            if (cur[j] == prev[i] + 1) { outp.Add(cur[j]); i++; j++; }
            else if (cur[j] < prev[i] + 1) j++;
            else i++;
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
}
