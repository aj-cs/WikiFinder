namespace SearchEngine.Core;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
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
    private readonly Dictionary<int, string> _docTitles = new(); // only needed for debug
    private readonly Dictionary<string, BitArray> _bitIndex = new(StringComparer.Ordinal);
    
    // BM25 specific fields
    private readonly Dictionary<int, int> _docLengths = new();
    private double _avgDocLength = 0;
    private int _totalDocs = 0;
    private double _k1 = 1.2; // BM25 parameter: term frequency saturation
    private double _b = 0.75; // BM25 parameter: document length normalization
    
    private bool _bitBuilt;
    private int _nextDocId;

    private bool _delta = true;
    // lock objects for thread safety
    private readonly object _mapLock = new object();
    private readonly object _statsLock = new object();
    private readonly object _bitsLock = new object();
            
    public void SetDeltaEncoding(bool on) => _delta = on;
    
    // methods to tune BM25 parameters
    public void SetBM25Params(double k1, double b)
    {
        if (k1 < 0) throw new ArgumentException("k1 must be non-negative");
        if (b < 0 || b > 1) throw new ArgumentException("b must be between 0 and 1");
        
        _k1 = k1;
        _b = b;
    }
    
    public (double k1, double b) GetBM25Params() => (_k1, _b);

    private bool _useBM25 = true; // default to using BM25

    public void SetBM25Enabled(bool enabled)
    {
        _useBM25 = enabled;
    }

    public bool IsBM25Enabled() => _useBM25;

    // ------------------------------------------------------------------------
    public void AddDocument(int docId, IEnumerable<Token> tokens)
    {
        // track document length for BM25
        var tokenList = tokens.ToList();
        int docLength = tokenList.Count;
        
        // Update document length stats (thread-safe)
        lock (_statsLock)
        {
            // update document length stats
            if (_docLengths.ContainsKey(docId))
            {
                // if we are updating a document, first remove its old length from the average
                _avgDocLength = (_avgDocLength * _totalDocs - _docLengths[docId]) / (_totalDocs - 1);
                _docLengths[docId] = docLength;
            }
            else
            {
                _docLengths[docId] = docLength;
                _totalDocs++;
            }
            
            // recalculate average document length
            _avgDocLength = _docLengths.Values.Sum() / (double)_totalDocs;
            
            if (docId >= _nextDocId)
            {
                _nextDocId = docId + 1;
            }
        }

        // group tokens by term to reduce lock contention
        var termGroups = tokenList
            .Where(t => !string.IsNullOrEmpty(t.Term))
            .GroupBy(t => t.Term)
            .ToList();

        // process each term group
        foreach (var group in termGroups)
        {
            string term = group.Key;
            var positions = group.Select(t => t.Position).OrderBy(p => p).ToList();
            
            lock (_mapLock)
            {
                if (!_map.TryGetValue(term, out var list))
                    _map[term] = list = new();

                if (list.Count > 0 && list[^1].DocId == docId)
                {
                    // update existing posting
                    int lastPos = list[^1].Positions[^1];
                    foreach (var pos in positions)
                    {
                        list[^1].Positions.Add(_delta ? pos - lastPos : pos);
                        lastPos = pos;
                    }
                    list[^1].Count += positions.Count;
                }
                else
                {
                    // Create a new posting with first position
                    var posting = new Posting(docId, positions[0]);
                    
                    // Add remaining positions
                    for (int i = 1; i < positions.Count; i++)
                    {
                        int lastPos = posting.Positions[^1];
                        posting.Positions.Add(_delta ? positions[i] - lastPos : positions[i]);
                        posting.Count++;
                    }
                    
                    list.Add(posting);
                }
            }
        }

        lock (_bitsLock)
        {
            _bitBuilt = false;
            BuildBits();
        }
    }

    // support for batch document processing
    public void AddDocumentsBatch(IEnumerable<(int docId, IEnumerable<Token> tokens)> documents)
    {
        // process documents in parallel
        var termPostingsMap = new ConcurrentDictionary<string, ConcurrentDictionary<int, List<int>>>();
        var docLengthsMap = new ConcurrentDictionary<int, int>();

        // first process tokens in parallel to build term postings
        Parallel.ForEach(documents, doc =>
        {
            var tokenList = doc.tokens.ToList();
            int docId = doc.docId;
            int docLength = tokenList.Count;
            
            // track document length
            docLengthsMap[docId] = docLength;
            
            // process each token
            foreach (var token in tokenList.Where(t => !string.IsNullOrEmpty(t.Term)))
            {
                var term = token.Term;
                var docPostings = termPostingsMap.GetOrAdd(term, _ => new ConcurrentDictionary<int, List<int>>());
                var positions = docPostings.GetOrAdd(docId, _ => new List<int>());
                
                lock (positions)
                {
                    positions.Add(token.Position);
                }
            }
        });

        // next update global data structures with appropriate locking
        lock (_statsLock)
        {
            // update document length stats
            foreach (var entry in docLengthsMap)
            {
                int docId = entry.Key;
                int docLength = entry.Value;
                
                if (_docLengths.ContainsKey(docId))
                {
                    // if we are updating a document, first remove its old length from the average
                    _avgDocLength = (_avgDocLength * _totalDocs - _docLengths[docId]) / (_totalDocs - 1);
                    _docLengths[docId] = docLength;
                }
                else
                {
                    _docLengths[docId] = docLength;
                    _totalDocs++;
                }
                
                if (docId >= _nextDocId)
                {
                    _nextDocId = docId + 1;
                }
            }
            
            // recalculate average document length
            _avgDocLength = _docLengths.Values.Sum() / (double)_totalDocs;
        }

        // third update inverted index with term postings
        lock (_mapLock)
        {
            foreach (var termEntry in termPostingsMap)
            {
                string term = termEntry.Key;
                var docPostings = termEntry.Value;
                
                if (!_map.TryGetValue(term, out var list))
                    _map[term] = list = new();
                
                foreach (var docEntry in docPostings)
                {
                    int docId = docEntry.Key;
                    var positions = docEntry.Value.OrderBy(p => p).ToList();
                    
                    if (list.Count > 0 && list[^1].DocId == docId)
                    {
                        // update existing posting
                        int lastPos = list[^1].Positions[^1];
                        foreach (var pos in positions)
                        {
                            list[^1].Positions.Add(_delta ? pos - lastPos : pos);
                            lastPos = pos;
                        }
                        list[^1].Count += positions.Count;
                    }
                    else
                    {
                        // create a new posting
                        if (positions.Count > 0)
                        {
                            var posting = new Posting(docId, positions[0]);
                            
                            // add remaining positions
                            for (int i = 1; i < positions.Count; i++)
                            {
                                int lastPos = posting.Positions[^1];
                                posting.Positions.Add(_delta ? positions[i] - lastPos : positions[i]);
                                posting.Count++;
                            }
                            
                            list.Add(posting);
                        }
                    }
                }
            }
        }

        // finally rebuild bit indices
        lock (_bitsLock)
        {
            _bitBuilt = false;
            BuildBits();
        }
    }

    public void RemoveDocument(int docId, IEnumerable<Token> tokens)
    {
        // update document length stats
        if (_docLengths.ContainsKey(docId))
        {
            // emove document length from average calculation
            if (_totalDocs > 1)
            {
                _avgDocLength = (_avgDocLength * _totalDocs - _docLengths[docId]) / (_totalDocs - 1);
            }
            else
            {
                _avgDocLength = 0;
            }
            
            _docLengths.Remove(docId);
            _totalDocs--;
        }
        
        // remove document from all term postings
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
        _docLengths.Clear();    // clear document lengths
        _avgDocLength = 0;      // reset average document length
        _totalDocs = 0;         // reset total document count
        _nextDocId = 0;         // reset doc-id counter
        _bitBuilt = false;      // mark bits as needing rebuild
    }
    
    // ------------------------------------------------------------------------
    // BM25 scoring function
    private double CalculateBM25Score(string term, int docId, int termFrequency)
    {
        if (!_useBM25) return 0; // Skip BM25 calculation if disabled

        if (!_map.TryGetValue(term, out var postings) || postings.Count == 0 || !_docLengths.TryGetValue(docId, out var docLength))
            return 0;
            
        // IDF component: log((N-n+0.5)/(n+0.5))
        double N = _totalDocs;
        double n = postings.Count; // number of documents containing the term
        double idf = Math.Log((N - n + 0.5) / (n + 0.5) + 1.0);
        
        // normalized term frequency
        double normalizedTF = termFrequency / (1.0 - _b + _b * (docLength / _avgDocLength));
        
        // BM25 score
        double score = idf * (normalizedTF * (_k1 + 1)) / (normalizedTF + _k1);
        
        return score;
    }

    // ------------------------------------------------------------------------
    public List<(int docId, int count)> ExactSearch(string searchStr)
    {
        string word = searchStr;
        if (!_map.TryGetValue(word, out var postings))
        {
            return new List<(int docId, int count)>();
        }

        // preallocation of result list with its exact capacity
        var results = new List<(int docId, double score)>(postings.Count);
        
        foreach (var posting in postings)
        {
            double score = CalculateBM25Score(word, posting.DocId, posting.Count);
            results.Add((posting.DocId, score));
        }

        // preallocation of final list with exact capacity
        var finalResults = new List<(int docId, int count)>(results.Count);
        foreach (var item in results.OrderByDescending(r => r.score))
        {
            finalResults.Add((item.docId, (int)(item.score * 1000))); // Scaled
        }
        
        return finalResults;
    }

    public List<(int docId, int count)> PhraseSearch(string phrase)
    {
        var words = phrase.Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0) return new List<(int docId, int count)>();

        if (words.Length == 1) return ExactSearch(words[0]);

        if (!_map.TryGetValue(words[0], out var first)) return new List<(int docId, int count)>();

        var candidates = first.ToDictionary(p => p.DocId,
                                            p => new List<int>(p.Positions));
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
        
        // score using combined BM25
        var docIds = candidates.Keys.ToList();
        var results = new List<(int docId, double score)>(docIds.Count);
        
        foreach (var docId in docIds)
        {
            double totalScore = 0;
            for (int i = 0; i < words.Length; i++)
            {
                if (_map.TryGetValue(words[i], out var termPostings))
                {
                    var posting = termPostings.FirstOrDefault(p => p.DocId == docId);
                    if (posting != null)
                    {
                        totalScore += CalculateBM25Score(words[i], docId, posting.Count);
                    }
                }
            }
            // boost phrase matches
            totalScore *= 1.2;
            results.Add((docId, totalScore));
        }
        
        // preallocation of final list with exact capacity
        var finalResults = new List<(int docId, int count)>(results.Count);
        foreach (var item in results.OrderByDescending(r => r.score))
        {
            finalResults.Add((item.docId, (int)(item.score * 1000))); // scaled
        }
        
        return finalResults;
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

        // collect matching doc IDs
        var docIds = new List<int>();
        for (int i = 0; i < acc.Length; i++)
            if (acc[i]) docIds.Add(i);
        
        if (docIds.Count == 0) return new List<(int docId, int count)>();
        
        // calculate BM25 scores for each matching document
        var results = new List<(int docId, double score)>(docIds.Count);
        foreach (var docId in docIds)
        {
            double totalScore = 0;
            foreach (var term in terms)
            {
                if (_map.TryGetValue(term, out var postings))
                {
                    var posting = postings.FirstOrDefault(p => p.DocId == docId);
                    if (posting != null)
                    {
                        totalScore += CalculateBM25Score(term, docId, posting.Count);
                    }
                }
            }
            results.Add((docId, totalScore));
        }
        
        // preallocation of final list with exact capacity
        var finalResults = new List<(int docId, int count)>(results.Count);
        foreach (var item in results.OrderByDescending(r => r.score))
        {
            finalResults.Add((item.docId, (int)(item.score * 1000))); // scaled
        }
        
        return finalResults;
    }

    private void BuildBits()
    {
        if (_bitBuilt) return;
        _bitIndex.Clear();
        
        foreach (var kv in _map)
        {
            var word = kv.Key;
            var list = kv.Value;
            var bits = new BitArray(_nextDocId);
            foreach (var p in list)
            {
                if (p.DocId < _nextDocId)
                    bits[p.DocId] = true;
            }
            _bitIndex[word] = bits;
        }
        _bitBuilt = true;
    }

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
    if (prev.Count > 0 && prev[0] == 0) prev = Decode(prev);
    if (cur.Count > 0 && cur[0] == 0) cur = Decode(cur);
    
    int i = 0, j = 0;
    while (i < prev.Count && j < cur.Count)
    {
        int target = prev[i] + 1;
        while (j < cur.Count && cur[j] < target)
        {
            j++;
        }
        if (j < cur.Count && cur[j] == target)
        {
            outp.Add(cur[j]);
        }
            i++;
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

    public List<(string word, List<int> docIds)> PrefixSearch(string prefix)
    {
        // estimate capacity to avoid resizing
        var estimatedMatches = Math.Min(20, _map.Count / 10); // rough guess
        var results = new List<(string word, List<int> docIds)>(estimatedMatches);
        
        foreach (var kvp in _map)
        {
            if (kvp.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                // pre-allocate docIds list to avoid resizing
                var docIds = new List<int>(kvp.Value.Count);
                foreach (var posting in kvp.Value)
                {
                    docIds.Add(posting.DocId);
                }
                results.Add((kvp.Key, docIds));
            }
        }
        return results;
    }

    public List<(int docId, int count)> BooleanSearchNaive(string expr)
    {
        var terms = expr.Split(new[] { "&&", "||" }, StringSplitOptions.RemoveEmptyEntries)
                       .Select(t => t.Trim())
                       .Where(t => !string.IsNullOrWhiteSpace(t))
                       .ToList();

        var operators = new List<string>();
        int i = 0;
        while (i < expr.Length - 1)
        {
            if (i + 2 <= expr.Length && expr.Substring(i, 2) == "&&")
            {
                operators.Add("&&");
                i += 2;
            }
            else if (i + 2 <= expr.Length && expr.Substring(i, 2) == "||")
            {
                operators.Add("||");
                i += 2;
            }
            else
            {
                i++;
            }
        }

        // pre-allocate with known capacity
        var docSets = new List<HashSet<int>>(terms.Count);
        foreach (var term in terms)
        {
            if (_map.TryGetValue(term, out var postings))
            {
                var docSet = new HashSet<int>(postings.Count);
                foreach (var posting in postings)
                {
                    docSet.Add(posting.DocId);
                }
                docSets.Add(docSet);
            }
            else
            {
                docSets.Add(new HashSet<int>());
            }
        }

        if (docSets.Count == 0) return new List<(int docId, int count)>();
        
        var result = docSets[0];
        for (int j = 0; j < operators.Count; j++)
        {
            if (operators[j] == "&&")
            {
                result.IntersectWith(docSets[j + 1]);
            }
            else // "||"
            {
                result.UnionWith(docSets[j + 1]);
            }
        }

        // calculate scores for matching documents
        var scoreResults = new List<(int docId, double score)>(result.Count);
        foreach (var docId in result)
        {
            double totalScore = 0;
            foreach (var term in terms)
            {
                if (_map.TryGetValue(term, out var postings))
                {
                    var posting = postings.FirstOrDefault(p => p.DocId == docId);
                    if (posting != null)
                    {
                        totalScore += CalculateBM25Score(term, docId, posting.Count);
                    }
                }
            }
            scoreResults.Add((docId, totalScore));
        }

        // preallocation of final list with exact capacity
        var finalResults = new List<(int docId, int count)>(scoreResults.Count);
        foreach (var item in scoreResults.OrderByDescending(r => r.score))
        {
            finalResults.Add((item.docId, (int)(item.score * 1000))); // scaled
        }
        
        return finalResults;
    }

}
