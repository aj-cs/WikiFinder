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
    public List<(int docId, int count)> ExactSearch(string searchStr)
    {
        string word = searchStr;
        if (!_map.TryGetValue(word, out var postings))
        {
            return new List<(int docId, int count)>();
        }

        // return document ids with their term counts
        return postings
            .OrderByDescending(p => p.Count)
            .Select(p => (p.DocId, p.Count))
            .ToList();
    }

    public List<(int docId, int count)> PhraseSearch(string phrase)
    {
        var words = phrase.Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0) return new List<(int docId, int count)>();

        if (words.Length == 1) return ExactSearch(words[0]);

        if (!_map.TryGetValue(words[0], out var first)) return new List<(int docId, int count)>();

        var candidates = first.ToDictionary(p => p.DocId,
                                            p => new List<int>(p.Positions));
        // track matching counts for scoring
        var matchCounts = first.ToDictionary(p => p.DocId, p => p.Count);
                                            
        for (int i = 1; i < words.Length; i++)
        {
            if (!_map.TryGetValue(words[i], out var next)) return new List<(int docId, int count)>();
            var nextSet = new Dictionary<int, List<int>>();
            var nextMatchCounts = new Dictionary<int, int>();
            
            foreach (var p in next)
            {
                if (!candidates.TryGetValue(p.DocId, out var prev)) continue;
                var valid = MergePositions(prev, p.Positions);
                if (valid.Count > 0) 
                {
                    nextSet[p.DocId] = valid;
                    // add count from this term if document matched
                    if (matchCounts.TryGetValue(p.DocId, out var prevCount))
                    {
                        nextMatchCounts[p.DocId] = prevCount + p.Count;
                    }
                    else
                    {
                        nextMatchCounts[p.DocId] = p.Count;
                    }
                }
            }
            candidates = nextSet;
            matchCounts = nextMatchCounts;
            if (candidates.Count == 0) return new List<(int docId, int count)>();
        }
        
        // return document ids with their term counts
        return matchCounts
            .OrderByDescending(kv => kv.Value)
            .Select(kv => (kv.Key, kv.Value))
            .ToList();
    }

    public List<(int docId, int count)> BooleanSearch(string expr)
    {
        BuildBits();

        BitArray? acc = null;
        string? op   = null;
        var terms = new List<string>();
        
        foreach (var token in expr.Split(' ', System.StringSplitOptions.RemoveEmptyEntries))
        {
            if (token is "&&" or "||") { op = token; continue; }
            
            terms.Add(token);
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
        
        // calculate term frequency for each matching document
        var docScores = new Dictionary<int, int>();
        foreach (var docId in docIds)
        {
            int totalCount = 0;
            foreach (var term in terms)
            {
                if (_map.TryGetValue(term, out var postings))
                {
                    var posting = postings.FirstOrDefault(p => p.DocId == docId);
                    if (posting != null)
                    {
                        totalCount += posting.Count;
                    }
                }
            }
            docScores[docId] = totalCount;
        }
        
        // return document ids with their term counts
        return docScores
            .OrderByDescending(kv => kv.Value)
            .Select(kv => (kv.Key, kv.Value))
            .ToList();
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

    private static List<int> MergePositions(List<int> prev, List<int> cur)
    {
        var outp = new List<int>();
        // delta decoder
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
}
