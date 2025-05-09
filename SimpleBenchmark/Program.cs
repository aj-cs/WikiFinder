using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;

namespace SearchEngine.Benchmarks
{
    [Config(typeof(BenchmarkConfig))]
    public class BenchmarkConfig : ManualConfig
    {
        public BenchmarkConfig()
        {
            AddExporter(CsvExporter.Default);
            AddJob(Job.Default.WithWarmupCount(1).WithIterationCount(3));
        }
    }

    [MemoryDiagnoser]
    public class DataStructureBenchmarks
    {
        [Params(
            "WestburyLab.wikicorp.201004_100KB.txt",
            "WestburyLab.wikicorp.201004_1MB.txt", 
            "WestburyLab.wikicorp.201004_5MB.txt",
            "WestburyLab.wikicorp.201004_10MB.txt",
            "WestburyLab.wikicorp.201004_20MB.txt",
            "WestburyLab.wikicorp.201004_50MB.txt",
            "WestburyLab.wikicorp.201004_100MB.txt",
            "WestburyLab.wikicorp.201004_200MB.txt",
            "WestburyLab.wikicorp.201004_400MB.txt"
        )]
        public string FileName { get; set; } = null!;

        private readonly string _textsPath = "/home/shierfall/Downloads/texts";
        private string _text = null!;
        private List<string> _uniqueTerms = null!;
        private List<string> _commonTerms = null!;

        [GlobalSetup]
        public void Setup()
        {
            string filePath = Path.Combine(_textsPath, FileName);
            
            // read the file
            _text = File.ReadAllText(filePath);
            
            // simple tokenization and normalization for benchmarks
            var tokens = SimplifyText(_text);
            _uniqueTerms = tokens.Distinct().ToList();
            
            // select some common terms for search benchmarks
            _commonTerms = new List<string> { "the", "and", "to", "for", "in" };
        }

        private static List<string> SimplifyText(string text)
        {
            // simple tokenization - split by whitespace and remove punctuation
            return text.ToLowerInvariant()
                .Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(word => new string(word.Where(c => char.IsLetterOrDigit(c)).ToArray()))
                .Where(word => !string.IsNullOrEmpty(word))
                .ToList();
        }

        #region InvertedIndex Benchmarks
        
        [Benchmark]
        public Dictionary<string, List<int>> BuildInvertedIndex()
        {
            var invertedIndex = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);
            var tokens = SimplifyText(_text);
            
            for (int i = 0; i < tokens.Count; i++)
            {
                string term = tokens[i];
                if (!invertedIndex.TryGetValue(term, out var positions))
                {
                    positions = new List<int>();
                    invertedIndex[term] = positions;
                }
                positions.Add(i);
            }
            
            return invertedIndex;
        }
        
        [Benchmark]
        public List<int> InvertedIndex_Search()
        {
            var invertedIndex = BuildInvertedIndex();
            var results = new List<int>();
            
            foreach (var term in _commonTerms)
            {
                if (invertedIndex.TryGetValue(term, out var positions))
                {
                    results.AddRange(positions);
                }
            }
            
            return results;
        }
        
        #endregion
        
        #region Trie Benchmarks
        
        // simple trie node for benchmarking
        public class TrieNode
        {
            public Dictionary<char, TrieNode> Children { get; } = new Dictionary<char, TrieNode>();
            public bool IsEndOfWord { get; set; }
            public List<int> Positions { get; } = new List<int>();
        }
        
        [Benchmark]
        public TrieNode BuildTrie()
        {
            var root = new TrieNode();
            var tokens = SimplifyText(_text);
            
            for (int i = 0; i < tokens.Count; i++)
            {
                string word = tokens[i];
                var current = root;
                
                foreach (char c in word)
                {
                    if (!current.Children.TryGetValue(c, out var node))
                    {
                        node = new TrieNode();
                        current.Children[c] = node;
                    }
                    current = node;
                }
                
                current.IsEndOfWord = true;
                current.Positions.Add(i);
            }
            
            return root;
        }
        
        [Benchmark]
        public List<int> Trie_Search()
        {
            var trie = BuildTrie();
            var results = new List<int>();
            
            foreach (var term in _commonTerms)
            {
                var current = trie;
                bool found = true;
                
                foreach (char c in term)
                {
                    if (!current.Children.TryGetValue(c, out var node))
                    {
                        found = false;
                        break;
                    }
                    current = node;
                }
                
                if (found && current.IsEndOfWord)
                {
                    results.AddRange(current.Positions);
                }
            }
            
            return results;
        }
        
        [Benchmark]
        public List<string> Trie_PrefixSearch()
        {
            var trie = BuildTrie();
            var results = new List<string>();
            
            foreach (var prefix in new[] { "th", "an", "to" })
            {
                var current = trie;
                bool found = true;
                
                foreach (char c in prefix)
                {
                    if (!current.Children.TryGetValue(c, out var node))
                    {
                        found = false;
                        break;
                    }
                    current = node;
                }
                
                if (found)
                {
                    // collect all words with this prefix
                    CollectWords(current, prefix, results);
                }
            }
            
            return results;
        }
        
        private void CollectWords(TrieNode node, string prefix, List<string> results)
        {
            if (node.IsEndOfWord)
            {
                results.Add(prefix);
            }
            
            foreach (var kvp in node.Children)
            {
                CollectWords(kvp.Value, prefix + kvp.Key, results);
            }
        }
        
        #endregion
        
        #region Bloom Filter Benchmarks
        
        // simple bloom filter implementation for benchmarking
        public class BloomFilter
        {
            private readonly bool[] _bits;
            private readonly int _hashFunctions;
            
            public BloomFilter(int capacity, double falsePositiveRate)
            {
                int m = CalculateOptimalSize(capacity, falsePositiveRate);
                int k = CalculateOptimalHashFunctions(capacity, m);
                
                _bits = new bool[m];
                _hashFunctions = k;
            }
            
            private static int CalculateOptimalSize(int n, double p)
            {
                return (int)Math.Ceiling(-n * Math.Log(p) / (Math.Log(2) * Math.Log(2)));
            }
            
            private static int CalculateOptimalHashFunctions(int n, int m)
            {
                return Math.Max(1, (int)Math.Round(m / (double)n * Math.Log(2)));
            }
            
            public void Add(string item)
            {
                for (int i = 0; i < _hashFunctions; i++)
                {
                    int hash = GetHash(item, i);
                    _bits[hash] = true;
                }
            }
            
            public bool MightContain(string item)
            {
                for (int i = 0; i < _hashFunctions; i++)
                {
                    int hash = GetHash(item, i);
                    if (!_bits[hash])
                    {
                        return false;
                    }
                }
                return true;
            }
            
            private int GetHash(string item, int seed)
            {
                // simple hash function for demo purposes
                int hash = item.GetHashCode() ^ seed;
                // ensure positive value
                hash = hash & 0x7FFFFFFF;
                return hash % _bits.Length;
            }
        }
        
        [Benchmark]
        public BloomFilter BuildBloomFilter()
        {
            var filter = new BloomFilter(_uniqueTerms.Count, 0.01);
            
            foreach (var term in _uniqueTerms)
            {
                filter.Add(term);
            }
            
            return filter;
        }
        
        [Benchmark]
        public int BloomFilter_CheckTerms()
        {
            var filter = BuildBloomFilter();
            int count = 0;
            
            foreach (var term in _commonTerms)
            {
                if (filter.MightContain(term))
                {
                    count++;
                }
            }
            
            return count;
        }
        
        #endregion
        
        #region Boolean Search Benchmarks
        
        [Benchmark]
        public List<int> BooleanSearch_AND()
        {
            var invertedIndex = BuildInvertedIndex();
            
            // search for documents containing both term1 AND term2
            string term1 = "the";
            string term2 = "and";
            
            if (!invertedIndex.TryGetValue(term1, out var positions1) || 
                !invertedIndex.TryGetValue(term2, out var positions2))
            {
                return new List<int>();
            }
            
            // naive implementation - find positions that are in both lists
            return positions1.Intersect(positions2).ToList();
        }
        
        [Benchmark]
        public List<int> BooleanSearch_OR()
        {
            var invertedIndex = BuildInvertedIndex();
            
            // search for documents containing either term1 OR term2
            string term1 = "the";
            string term2 = "and";
            
            var results = new HashSet<int>();
            
            if (invertedIndex.TryGetValue(term1, out var positions1))
            {
                foreach (var pos in positions1)
                {
                    results.Add(pos);
                }
            }
            
            if (invertedIndex.TryGetValue(term2, out var positions2))
            {
                foreach (var pos in positions2)
                {
                    results.Add(pos);
                }
            }
            
            return results.ToList();
        }
        
        #endregion
        
        #region Phrase Search Benchmarks
        
        [Benchmark]
        public List<int> PhraseSearch()
        {
            var invertedIndex = BuildInvertedIndex();
            var results = new List<int>();
            
            // search for the phrase "in the"
            string term1 = "in";
            string term2 = "the";
            
            if (!invertedIndex.TryGetValue(term1, out var positions1) || 
                !invertedIndex.TryGetValue(term2, out var positions2))
            {
                return results;
            }
            
            // find adjacent positions
            foreach (var pos1 in positions1)
            {
                if (positions2.Contains(pos1 + 1))
                {
                    results.Add(pos1);
                }
            }
            
            return results;
        }
        
        #endregion
    }

    /// <summary>
    /// specialized benchmark class for comparing search operations between invertedindex and trie
    /// </summary>
    [MemoryDiagnoser]
    public class SearchOperationsBenchmark
    {
        [Params(
            "WestburyLab.wikicorp.201004_100KB.txt",
            "WestburyLab.wikicorp.201004_1MB.txt", 
            "WestburyLab.wikicorp.201004_5MB.txt",
            "WestburyLab.wikicorp.201004_20MB.txt",
            "WestburyLab.wikicorp.201004_100MB.txt"
        )]
        public string FileName { get; set; } = null!;
        
        [Params("the", "and", "to", "history", "computer")]
        public string SearchTerm { get; set; } = null!;
        
        [Params("th", "an", "co", "his", "com")]
        public string PrefixTerm { get; set; } = null!;

        [Params("the and", "in the", "of a", "history of", "for example")]
        public string PhraseTerm { get; set; } = null!;

        [Params("the || and", "history && of", "in || the", "computer && science")]
        public string BooleanTerm { get; set; } = null!;

        private readonly string _textsPath = "/home/shierfall/Downloads/texts";
        private string _text = null!;
        private Dictionary<string, List<int>> _invertedIndex = null!;
        private DataStructureBenchmarks.TrieNode _trie = null!;

        [GlobalSetup]
        public void Setup()
        {
            string filePath = Path.Combine(_textsPath, FileName);
            
            // read the file
            _text = File.ReadAllText(filePath);
            
            // pre-build both data structures
            var tokens = SimplifyText(_text);
            
            // build inverted index
            _invertedIndex = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < tokens.Count; i++)
            {
                string term = tokens[i];
                if (!_invertedIndex.TryGetValue(term, out var positions))
                {
                    positions = new List<int>();
                    _invertedIndex[term] = positions;
                }
                positions.Add(i);
            }
            
            // build trie
            _trie = new DataStructureBenchmarks.TrieNode();
            for (int i = 0; i < tokens.Count; i++)
            {
                string word = tokens[i];
                var current = _trie;
                
                foreach (char c in word)
                {
                    if (!current.Children.TryGetValue(c, out var node))
                    {
                        node = new DataStructureBenchmarks.TrieNode();
                        current.Children[c] = node;
                    }
                    current = node;
                }
                
                current.IsEndOfWord = true;
                current.Positions.Add(i);
            }
        }

        private static List<string> SimplifyText(string text)
        {
            // simple tokenization - split by whitespace and remove punctuation
            return text.ToLowerInvariant()
                .Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(word => new string(word.Where(c => char.IsLetterOrDigit(c)).ToArray()))
                .Where(word => !string.IsNullOrEmpty(word))
                .ToList();
        }
        
        [Benchmark]
        public List<int> InvertedIndex_ExactSearch()
        {
            if (_invertedIndex.TryGetValue(SearchTerm, out var positions))
            {
                return positions;
            }
            return new List<int>();
        }
        
        [Benchmark]
        public List<int> Trie_ExactSearch()
        {
            var current = _trie;
            
            foreach (char c in SearchTerm)
            {
                if (!current.Children.TryGetValue(c, out var node))
                {
                    return new List<int>();
                }
                current = node;
            }
            
            return current.IsEndOfWord ? current.Positions : new List<int>();
        }
        
        [Benchmark]
        public List<string> InvertedIndex_PrefixSearch()
        {
            // for inverted index, we need to search all keys for the prefix
            return _invertedIndex.Keys
                .Where(key => key.StartsWith(PrefixTerm, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
        
        [Benchmark]
        public List<string> Trie_PrefixSearch()
        {
            var results = new List<string>();
            var current = _trie;
            
            // navigate to the prefix node
            foreach (char c in PrefixTerm)
            {
                if (!current.Children.TryGetValue(c, out var node))
                {
                    return results; // prefix not found
                }
                current = node;
            }
            
            // collect all words starting with the prefix
            CollectWords(current, PrefixTerm, results);
            return results;
        }
        
        [Benchmark]
        public List<int> InvertedIndex_PhraseSearch()
        {
            var results = new List<int>();
            var terms = PhraseTerm.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            
            if (terms.Length == 0)
                return results;
                
            if (terms.Length == 1)
                return InvertedIndex_ExactSearch(); // reuse exact search for single term
                
            // check if all terms exist in the index
            var termPositions = new List<List<int>>();
            foreach (var term in terms)
            {
                if (!_invertedIndex.TryGetValue(term, out var positions))
                    return results; // one term doesn't exist, no matches possible
                    
                termPositions.Add(positions);
            }
            
            // find positions where terms appear consecutively
            foreach (var pos in termPositions[0])
            {
                bool isPhrase = true;
                for (int i = 1; i < terms.Length; i++)
                {
                    if (!termPositions[i].Contains(pos + i))
                    {
                        isPhrase = false;
                        break;
                    }
                }
                
                if (isPhrase)
                    results.Add(pos);
            }
            
            return results;
        }
        
        [Benchmark]
        public List<int> Trie_PhraseSearch()
        {
            var results = new List<int>();
            var terms = PhraseTerm.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            
            if (terms.Length == 0)
                return results;
                
            if (terms.Length == 1)
                return Trie_ExactSearch(); // reuse exact search for single term
                
            // find positions for each term
            var termPositions = new List<List<int>>();
            foreach (var term in terms)
            {
                var current = _trie;
                bool found = true;
                
                foreach (char c in term)
                {
                    if (!current.Children.TryGetValue(c, out var node))
                    {
                        found = false;
                        break;
                    }
                    current = node;
                }
                
                if (!found || !current.IsEndOfWord)
                    return results; // one term doesn't exist, no matches possible
                    
                termPositions.Add(current.Positions);
            }
            
            // find positions where terms appear consecutively
            foreach (var pos in termPositions[0])
            {
                bool isPhrase = true;
                for (int i = 1; i < terms.Length; i++)
                {
                    if (!termPositions[i].Contains(pos + i))
                    {
                        isPhrase = false;
                        break;
                    }
                }
                
                if (isPhrase)
                    results.Add(pos);
            }
            
            return results;
        }
        
        [Benchmark]
        public List<int> InvertedIndex_BooleanSearch()
        {
            var parts = BooleanTerm.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            
            if (parts.Length < 3 || (parts[1] != "&&" && parts[1] != "||"))
                return new List<int>();
                
            string term1 = parts[0];
            string op = parts[1];
            string term2 = parts[2];
            
            if (!_invertedIndex.TryGetValue(term1, out var positions1))
                positions1 = new List<int>();
                
            if (!_invertedIndex.TryGetValue(term2, out var positions2))
                positions2 = new List<int>();
                
            if (op == "&&")
                return positions1.Intersect(positions2).ToList();
            else // op == "||"
                return positions1.Union(positions2).ToList();
        }
        
        [Benchmark]
        public List<int> Trie_BooleanSearch()
        {
            var parts = BooleanTerm.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            
            if (parts.Length < 3 || (parts[1] != "&&" && parts[1] != "||"))
                return new List<int>();
                
            string term1 = parts[0];
            string op = parts[1];
            string term2 = parts[2];
            
            // find positions for term1
            var positions1 = new List<int>();
            var current1 = _trie;
            bool found1 = true;
            
            foreach (char c in term1)
            {
                if (!current1.Children.TryGetValue(c, out var node))
                {
                    found1 = false;
                    break;
                }
                current1 = node;
            }
            
            if (found1 && current1.IsEndOfWord)
                positions1 = current1.Positions;
                
            // find positions for term2
            var positions2 = new List<int>();
            var current2 = _trie;
            bool found2 = true;
            
            foreach (char c in term2)
            {
                if (!current2.Children.TryGetValue(c, out var node))
                {
                    found2 = false;
                    break;
                }
                current2 = node;
            }
            
            if (found2 && current2.IsEndOfWord)
                positions2 = current2.Positions;
                
            if (op == "&&")
                return positions1.Intersect(positions2).ToList();
            else // op == "||"
                return positions1.Union(positions2).ToList();
        }
        
        private void CollectWords(DataStructureBenchmarks.TrieNode node, string prefix, List<string> results)
        {
            if (node.IsEndOfWord)
            {
                results.Add(prefix);
            }
            
            foreach (var kvp in node.Children)
            {
                CollectWords(kvp.Value, prefix + kvp.Key, results);
            }
        }
    }
    
    public class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                if (args[0] == "--search-comparison")
                {
                    BenchmarkRunner.Run<SearchOperationsBenchmark>();
                }
                else if (args[0] == "--features-comparison")
                {
                    BenchmarkRunner.Run<FeaturesComparisonBenchmark>();
                }
                else if (args[0] == "--delta-encoding")
                {
                    BenchmarkRunner.Run<DeltaEncodingBenchmark>();
                }
                else if (args[0] == "--boolean-search")
                {
                    BenchmarkRunner.Run<BooleanSearchBenchmark>();
                }
                else if (args[0] == "--text-analysis")
                {
                    BenchmarkRunner.Run<TextAnalysisBenchmark>();
                }
            }
            else
            {
                BenchmarkRunner.Run<DataStructureBenchmarks>();
            }
        }
    }
    
    /// <summary>
    /// benchmark class that specifically compares different implementations of boolean search
    /// </summary>
    [MemoryDiagnoser]
    public class BooleanSearchBenchmark
    {
        [Params(
            "WestburyLab.wikicorp.201004_1MB.txt", 
            "WestburyLab.wikicorp.201004_10MB.txt",
            "WestburyLab.wikicorp.201004_50MB.txt"
        )]
        public string FileName { get; set; } = null!;
        
        [Params("the && and", "history && of", "to || from", "computer || science")]
        public string BooleanQuery { get; set; } = null!;
        
        private readonly string _textsPath = "/home/shierfall/Downloads/texts";
        private string _text = null!;
        private Dictionary<string, List<int>> _invertedIndex = null!;
        private Dictionary<string, BitArray> _bitIndex = null!;
        private int _nextDocId = 0;
        
        [GlobalSetup]
        public void Setup()
        {
            string filePath = Path.Combine(_textsPath, FileName);
            
            // read the file
            _text = File.ReadAllText(filePath);
            
            // build inverted index
            _invertedIndex = BuildInvertedIndex();
            
            // build bit index for bitset-based searches
            _bitIndex = BuildBitIndex();
        }
        
        private Dictionary<string, List<int>> BuildInvertedIndex()
        {
            var invertedIndex = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);
            var tokens = SimplifyText(_text);
            
            // simulate processing in documents of 1000 tokens each
            int docSize = 1000; // tokens per document
            _nextDocId = 0;
            
            for (int i = 0; i < tokens.Count; i += docSize)
            {
                int docId = _nextDocId++;
                
                for (int j = 0; j < docSize && i + j < tokens.Count; j++)
                {
                    string term = tokens[i + j];
                    
                    // get or create postings list
                    if (!invertedIndex.TryGetValue(term, out var postings))
                    {
                        postings = new List<int>();
                        invertedIndex[term] = postings;
                    }
                    
                    // add document ID if not already there
                    if (!postings.Contains(docId))
                    {
                        postings.Add(docId);
                    }
                }
            }
            
            return invertedIndex;
        }
        
        private Dictionary<string, BitArray> BuildBitIndex()
        {
            Dictionary<string, BitArray> bitIndex = new Dictionary<string, BitArray>(StringComparer.OrdinalIgnoreCase);
            
            foreach (var entry in _invertedIndex)
            {
                var bits = new BitArray(_nextDocId);
                foreach (var docId in entry.Value)
                {
                    bits.Set(docId, true);
                }
                bitIndex[entry.Key] = bits;
            }
            
            return bitIndex;
        }
        
        private static List<string> SimplifyText(string text)
        {
            // simple tokenization - split by whitespace and remove punctuation
            return text.ToLowerInvariant()
                .Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(word => new string(word.Where(c => char.IsLetterOrDigit(c)).ToArray()))
                .Where(word => !string.IsNullOrEmpty(word))
                .ToList();
        }
        
        /// <summary>
        /// benchmark the naive approach to boolean search that uses hashset operations
        /// </summary>
        [Benchmark]
        public List<int> BooleanSearch_Naive()
        {
            string[] parts = BooleanQuery.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 3 || (parts[1] != "&&" && parts[1] != "||"))
                return new List<int>();
                
            string term1 = parts[0];
            string op = parts[1];
            string term2 = parts[2];
            
            if (!_invertedIndex.TryGetValue(term1, out var positions1))
                positions1 = new List<int>();
                
            if (!_invertedIndex.TryGetValue(term2, out var positions2))
                positions2 = new List<int>();
                
            HashSet<int> set1 = new HashSet<int>(positions1);
            HashSet<int> set2 = new HashSet<int>(positions2);
            
            if (op == "&&")
            {
                set1.IntersectWith(set2);
                return set1.ToList();
            }
            else // op == "||"
            {
                set1.UnionWith(set2);
                return set1.ToList();
            }
        }
        
        /// <summary>
        /// benchmark the bitarray-based approach to boolean search
        /// </summary>
        [Benchmark]
        public List<int> BooleanSearch_BitArray()
        {
            string[] parts = BooleanQuery.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 3 || (parts[1] != "&&" && parts[1] != "||"))
                return new List<int>();
                
            string term1 = parts[0];
            string op = parts[1];
            string term2 = parts[2];
            
            // use empty bitarray if term not found
            if (!_bitIndex.TryGetValue(term1, out var bits1))
                bits1 = new BitArray(_nextDocId);
                
            if (!_bitIndex.TryGetValue(term2, out var bits2))
                bits2 = new BitArray(_nextDocId);
                
            // create result bitarray
            BitArray resultBits = (BitArray)bits1.Clone();
            
            if (op == "&&")
                resultBits.And(bits2);
            else // op == "||"
                resultBits.Or(bits2);
                
            // convert bitarray to list of matching document IDs
            var results = new List<int>();
            for (int i = 0; i < _nextDocId; i++)
            {
                if (resultBits[i])
                    results.Add(i);
            }
            
            return results;
        }
        
        /// <summary>
        /// benchmark a more complex boolean query with 4 terms
        /// </summary>
        [Benchmark]
        public List<int> BooleanSearch_Complex_Naive()
        {
            // parse the query first
            string[] parts = BooleanQuery.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            string term1 = parts[0];
            
            // for testing complex queries, use term3 && term4 for AND queries,
            // and term3 || term4 for OR queries
            string term3 = "data";
            string term4 = "computer";
            
            // get document lists for all terms
            if (!_invertedIndex.TryGetValue(term1, out var docs1))
                docs1 = new List<int>();
                
            if (!_invertedIndex.TryGetValue(parts[2], out var docs2))
                docs2 = new List<int>();
                
            if (!_invertedIndex.TryGetValue(term3, out var docs3))
                docs3 = new List<int>();
                
            if (!_invertedIndex.TryGetValue(term4, out var docs4))
                docs4 = new List<int>();
                
            // first evaluate term3 && term4 or term3 || term4
            HashSet<int> intermediateResult = new HashSet<int>();
            if (parts[1] == "&&")
            {
                // intersect docs3 and docs4
                intermediateResult = new HashSet<int>(docs3);
                intermediateResult.IntersectWith(docs4);
            }
            else
            {
                // union docs3 and docs4
                intermediateResult = new HashSet<int>(docs3);
                intermediateResult.UnionWith(docs4);
            }
            
            // now combine with the first term
            HashSet<int> finalResult = new HashSet<int>(docs1);
            if (parts[1] == "&&")
            {
                // AND with the intermediate result
                finalResult.IntersectWith(docs2);
                finalResult.IntersectWith(intermediateResult);
            }
            else
            {
                // OR with the intermediate result
                finalResult.UnionWith(docs2);
                finalResult.UnionWith(intermediateResult);
            }
            
            return finalResult.ToList();
        }
        
        /// <summary>
        /// benchmark a more complex boolean query with bitarrays
        /// </summary>
        [Benchmark]
        public List<int> BooleanSearch_Complex_BitArray()
        {
            // parse the query
            string[] parts = BooleanQuery.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            string term1 = parts[0];
            
            // for testing complex queries, use term3 && term4 for AND queries, 
            // and term3 || term4 for OR queries
            string term3 = "data";
            string term4 = "computer";
            
            // get bitarrays for all terms
            if (!_bitIndex.TryGetValue(term1, out var bits1))
                bits1 = new BitArray(_nextDocId);
                
            if (!_bitIndex.TryGetValue(parts[2], out var bits2))
                bits2 = new BitArray(_nextDocId);
                
            if (!_bitIndex.TryGetValue(term3, out var bits3))
                bits3 = new BitArray(_nextDocId);
                
            if (!_bitIndex.TryGetValue(term4, out var bits4))
                bits4 = new BitArray(_nextDocId);
                
            // first evaluate term3 && term4 or term3 || term4
            BitArray intermediateResult = (BitArray)bits3.Clone();
            if (parts[1] == "&&")
                intermediateResult.And(bits4);
            else
                intermediateResult.Or(bits4);
                
            // now combine with the first two terms
            BitArray finalResult = (BitArray)bits1.Clone();
            if (parts[1] == "&&")
            {
                finalResult.And(bits2);
                finalResult.And(intermediateResult);
            }
            else
            {
                finalResult.Or(bits2);
                finalResult.Or(intermediateResult);
            }
            
            // convert to list of document IDs
            var results = new List<int>();
            for (int i = 0; i < _nextDocId; i++)
            {
                if (finalResult[i])
                    results.Add(i);
            }
            
            return results;
        }
    }
    
    [MemoryDiagnoser]
    public class DeltaEncodingBenchmark
    {
        [Params(
            "WestburyLab.wikicorp.201004_1MB.txt", 
            "WestburyLab.wikicorp.201004_10MB.txt",
            "WestburyLab.wikicorp.201004_50MB.txt",
            "WestburyLab.wikicorp.201004_100MB.txt"
        )]
        public string FileName { get; set; } = null!;
        
        private readonly string _textsPath = "/home/shierfall/Downloads/texts";
        private string _text = null!;
        private List<string> _searchTerms = null!;
        
        // positions for each document
        private Dictionary<int, List<int>> _docPositions = null!;
        
        [GlobalSetup]
        public void Setup()
        {
            string filePath = Path.Combine(_textsPath, FileName);
            
            // read the file
            _text = File.ReadAllText(filePath);
            
            // simple tokenization and normalization
            var tokens = SimplifyText(_text);
            
            // create document structure (simulate multiple documents)
            _docPositions = new Dictionary<int, List<int>>();
            int docSize = 1000; // tokens per document
            int docId = 0;
            
            for (int i = 0; i < tokens.Count; i += docSize)
            {
                var positions = new List<int>();
                for (int j = 0; j < docSize && i + j < tokens.Count; j++)
                {
                    positions.Add(j); // position within document
                }
                _docPositions[docId++] = positions;
            }
            
            // select search terms
            _searchTerms = new List<string> { "the", "and", "to", "of", "in" };
        }
        
        private static List<string> SimplifyText(string text)
        {
            // simple tokenization - split by whitespace and remove punctuation
            return text.ToLowerInvariant()
                .Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(word => new string(word.Where(c => char.IsLetterOrDigit(c)).ToArray()))
                .Where(word => !string.IsNullOrEmpty(word))
                .ToList();
        }
        
        #region Delta Encoding Benchmarks
        
        [Benchmark]
        public Dictionary<string, Dictionary<int, List<int>>> BuildInvertedIndexWithoutDelta()
        {
            var index = new Dictionary<string, Dictionary<int, List<int>>>(StringComparer.OrdinalIgnoreCase);
            var tokens = SimplifyText(_text);
            
            // simulate processing documents
            int docSize = 1000; // tokens per document
            int docId = 0;
            
            for (int i = 0; i < tokens.Count; i += docSize)
            {
                for (int j = 0; j < docSize && i + j < tokens.Count; j++)
                {
                    string term = tokens[i + j];
                    
                    // get or create term dictionary
                    if (!index.TryGetValue(term, out var postings))
                    {
                        postings = new Dictionary<int, List<int>>();
                        index[term] = postings;
                    }
                    
                    // get or create document positions list
                    if (!postings.TryGetValue(docId, out var positions))
                    {
                        positions = new List<int>();
                        postings[docId] = positions;
                    }
                    
                    // store absolute position
                    positions.Add(j);
                }
                docId++;
            }
            
            return index;
        }
        
        [Benchmark]
        public Dictionary<string, Dictionary<int, List<int>>> BuildInvertedIndexWithDelta()
        {
            var index = new Dictionary<string, Dictionary<int, List<int>>>(StringComparer.OrdinalIgnoreCase);
            var tokens = SimplifyText(_text);
            
            // simulate processing documents
            int docSize = 1000; // tokens per document
            int docId = 0;
            
            for (int i = 0; i < tokens.Count; i += docSize)
            {
                // track last position for each term in this document
                var lastPositions = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                
                for (int j = 0; j < docSize && i + j < tokens.Count; j++)
                {
                    string term = tokens[i + j];
                    
                    // get or create term dictionary
                    if (!index.TryGetValue(term, out var postings))
                    {
                        postings = new Dictionary<int, List<int>>();
                        index[term] = postings;
                    }
                    
                    // get or create document positions list
                    if (!postings.TryGetValue(docId, out var positions))
                    {
                        positions = new List<int>();
                        postings[docId] = positions;
                    }
                    
                    // store delta-encoded position
                    if (!lastPositions.TryGetValue(term, out var lastPos))
                    {
                        // first occurrence in this document - store absolute position
                        positions.Add(j);
                    }
                    else
                    {
                        // store difference from last position
                        positions.Add(j - lastPos);
                    }
                    
                    lastPositions[term] = j;
                }
                docId++;
            }
            
            return index;
        }
        
        [Benchmark]
        public int SearchInvertedIndexWithoutDelta()
        {
            var index = BuildInvertedIndexWithoutDelta();
            int count = 0;
            
            foreach (var term in _searchTerms)
            {
                if (index.TryGetValue(term, out var postings))
                {
                    foreach (var docPositions in postings.Values)
                    {
                        count += docPositions.Count;
                    }
                }
            }
            
            return count;
        }
        
        [Benchmark]
        public int SearchInvertedIndexWithDelta()
        {
            var index = BuildInvertedIndexWithDelta();
            int count = 0;
            
            foreach (var term in _searchTerms)
            {
                if (index.TryGetValue(term, out var postings))
                {
                    foreach (var docPositions in postings.Values)
                    {
                        // we need to decode deltas for real usage
                        var decoded = DecodeDelta(docPositions);
                        count += decoded.Count;
                    }
                }
            }
            
            return count;
        }
        
        private static List<int> DecodeDelta(List<int> deltas)
        {
            var decoded = new List<int>(deltas.Count);
            int current = 0;
            
            foreach (var delta in deltas)
            {
                current += delta;
                decoded.Add(current);
            }
            
            return decoded;
        }
        
        [Benchmark]
        public long MeasureMemoryUsageWithoutDelta()
        {
            var index = BuildInvertedIndexWithoutDelta();
            long size = 0;
            
            // estimate memory usage by counting integers stored
            foreach (var postings in index.Values)
            {
                foreach (var positions in postings.Values)
                {
                    // each position is an int (4 bytes)
                    size += positions.Count * sizeof(int);
                }
            }
            
            return size;
        }
        
        [Benchmark]
        public long MeasureMemoryUsageWithDelta()
        {
            var index = BuildInvertedIndexWithDelta();
            long size = 0;
            
            // estimate memory usage by counting integers stored
            foreach (var postings in index.Values)
            {
                foreach (var positions in postings.Values)
                {
                    // each position is an int (4 bytes)
                    size += positions.Count * sizeof(int);
                }
            }
            
            return size;
        }
        
        #endregion
    }
    
    [MemoryDiagnoser]
    public class FeaturesComparisonBenchmark
    {
        [Params(
            "WestburyLab.wikicorp.201004_1MB.txt", 
            "WestburyLab.wikicorp.201004_10MB.txt",
            "WestburyLab.wikicorp.201004_50MB.txt"
        )]
        public string FileName { get; set; } = null!;
        
        private readonly string _textsPath = "/home/shierfall/Downloads/texts";
        private string _text = null!;
        private List<string> _searchTerms = null!;
        private List<string> _nonExistentTerms = null!;
        
        [GlobalSetup]
        public void Setup()
        {
            string filePath = Path.Combine(_textsPath, FileName);
            
            // read the file
            _text = File.ReadAllText(filePath);
            
            // select search terms
            _searchTerms = new List<string> { "the", "and", "to", "of", "in" };
            
            // terms that don't exist in the text
            _nonExistentTerms = new List<string> { 
                "xyzabc", "nonexistent123", "unknownterm", "notfoundhere", "absentword" 
            };
        }
        
        #region Bloom Filter Benchmarks
        
        [Benchmark]
        public bool[] BloomFilter_FalsePositiveRate()
        {
            var tokens = SimplifyText(_text);
            var uniqueTerms = tokens.Distinct().ToList();
            
            // test different false positive rates
            double[] rates = { 0.01, 0.001, 0.0001 };
            bool[] results = new bool[_nonExistentTerms.Count * rates.Length];
            
            int resultIndex = 0;
            foreach (double rate in rates)
            {
                var filter = new DataStructureBenchmarks.BloomFilter(uniqueTerms.Count, rate);
                
                // add all terms
                foreach (var term in uniqueTerms)
                {
                    filter.Add(term);
                }
                
                // test for false positives
                foreach (var term in _nonExistentTerms)
                {
                    results[resultIndex++] = filter.MightContain(term);
                }
            }
            
            return results;
        }
        
        [Benchmark]
        public int[] BloomFilter_MemoryUsage()
        {
            var tokens = SimplifyText(_text);
            var uniqueTerms = tokens.Distinct().ToList();
            
            // test different false positive rates
            double[] rates = { 0.01, 0.001, 0.0001 };
            int[] bitSizes = new int[rates.Length];
            
            for (int i = 0; i < rates.Length; i++)
            {
                int m = CalculateOptimalSize(uniqueTerms.Count, rates[i]);
                bitSizes[i] = m;
            }
            
            return bitSizes;
        }
        
        private static int CalculateOptimalSize(int n, double p)
        {
            return (int)Math.Ceiling(-n * Math.Log(p) / (Math.Log(2) * Math.Log(2)));
        }
        
        private static List<string> SimplifyText(string text)
        {
            // simple tokenization - split by whitespace and remove punctuation
            return text.ToLowerInvariant()
                .Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(word => new string(word.Where(c => char.IsLetterOrDigit(c)).ToArray()))
                .Where(word => !string.IsNullOrEmpty(word))
                .ToList();
        }
        
        #endregion
    }

    /// <summary>
    /// benchmark class for measuring performance of various text analysis filters
    /// </summary>
    [MemoryDiagnoser]
    public class TextAnalysisBenchmark
    {
        [Params(
            "WestburyLab.wikicorp.201004_1MB.txt", 
            "WestburyLab.wikicorp.201004_10MB.txt",
            "WestburyLab.wikicorp.201004_50MB.txt"
        )]
        public string FileName { get; set; } = null!;
        
        private readonly string _textsPath = "/home/shierfall/Downloads/texts";
        private string _text = null!;
        
        // common stopwords for filtering
        private static readonly HashSet<string> _stopwords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "a", "an", "the", "and", "or", "but", "is", "are", "was", "were",
            "to", "in", "on", "at", "for", "with", "of", "by", "as", "that",
            "this", "these", "those", "it", "its", "they", "them", "their",
            "he", "she", "his", "her", "him", "i", "we", "you", "me", "us"
        };
        
        [GlobalSetup]
        public void Setup()
        {
            string filePath = Path.Combine(_textsPath, FileName);
            _text = File.ReadAllText(filePath);
        }
        
        /// <summary>
        /// basic tokenization without any filtering
        /// </summary>
        [Benchmark(Baseline = true)]
        public List<string> BasicTokenization()
        {
            return _text.ToLowerInvariant()
                .Split(new[] { ' ', '\t', '\n', '\r', '.', ',', ';', ':', '!', '?', '(', ')', '[', ']', '{', '}', '\"', '\'', '/', '\\' }, 
                       StringSplitOptions.RemoveEmptyEntries)
                .ToList();
        }
        
        /// <summary>
        /// tokenization with stopword filtering
        /// </summary>
        [Benchmark]
        public List<string> StopwordFiltering()
        {
            return _text.ToLowerInvariant()
                .Split(new[] { ' ', '\t', '\n', '\r', '.', ',', ';', ':', '!', '?', '(', ')', '[', ']', '{', '}', '\"', '\'', '/', '\\' }, 
                       StringSplitOptions.RemoveEmptyEntries)
                .Where(word => !_stopwords.Contains(word))
                .ToList();
        }
        
        /// <summary>
        /// tokenization with alphanumeric filtering (keeping only letters and numbers)
        /// </summary>
        [Benchmark]
        public List<string> AlphanumericFiltering()
        {
            return _text.ToLowerInvariant()
                .Split(new[] { ' ', '\t', '\n', '\r', '.', ',', ';', ':', '!', '?', '(', ')', '[', ']', '{', '}', '\"', '\'', '/', '\\' }, 
                       StringSplitOptions.RemoveEmptyEntries)
                .Select(word => new string(word.Where(c => char.IsLetterOrDigit(c)).ToArray()))
                .Where(word => !string.IsNullOrEmpty(word))
                .ToList();
        }
        
        /// <summary>
        /// tokenization with minimum length filtering (only words with length >= 3)
        /// </summary>
        [Benchmark]
        public List<string> MinLengthFiltering()
        {
            return _text.ToLowerInvariant()
                .Split(new[] { ' ', '\t', '\n', '\r', '.', ',', ';', ':', '!', '?', '(', ')', '[', ']', '{', '}', '\"', '\'', '/', '\\' }, 
                       StringSplitOptions.RemoveEmptyEntries)
                .Where(word => word.Length >= 3)
                .ToList();
        }
        
        /// <summary>
        /// combination of stopword and alphanumeric filtering
        /// </summary>
        [Benchmark]
        public List<string> StopwordAndAlphanumeric()
        {
            return _text.ToLowerInvariant()
                .Split(new[] { ' ', '\t', '\n', '\r', '.', ',', ';', ':', '!', '?', '(', ')', '[', ']', '{', '}', '\"', '\'', '/', '\\' }, 
                       StringSplitOptions.RemoveEmptyEntries)
                .Select(word => new string(word.Where(c => char.IsLetterOrDigit(c)).ToArray()))
                .Where(word => !string.IsNullOrEmpty(word) && !_stopwords.Contains(word))
                .ToList();
        }
        
        /// <summary>
        /// tokenization with basic stemming (just removing common endings)
        /// </summary>
        [Benchmark]
        public List<string> BasicStemming()
        {
            var tokens = _text.ToLowerInvariant()
                .Split(new[] { ' ', '\t', '\n', '\r', '.', ',', ';', ':', '!', '?', '(', ')', '[', ']', '{', '}', '\"', '\'', '/', '\\' }, 
                       StringSplitOptions.RemoveEmptyEntries);
                       
            var stemmed = new List<string>(tokens.Length);
            foreach (var token in tokens)
            {
                string stem = SimpleStem(token);
                if (!string.IsNullOrEmpty(stem))
                {
                    stemmed.Add(stem);
                }
            }
            
            return stemmed;
        }
        
        /// <summary>
        /// complete filtering pipeline: tokenize, lowercase, remove non-alphanumeric,
        /// remove stopwords, apply stemming, and filter by minimum length
        /// </summary>
        [Benchmark]
        public List<string> CompleteFilteringPipeline()
        {
            var tokens = _text.ToLowerInvariant()
                .Split(new[] { ' ', '\t', '\n', '\r', '.', ',', ';', ':', '!', '?', '(', ')', '[', ']', '{', '}', '\"', '\'', '/', '\\' }, 
                       StringSplitOptions.RemoveEmptyEntries);
                       
            var processed = new List<string>(tokens.Length);
            foreach (var token in tokens)
            {
                // clean up non-alphanumeric characters
                string cleaned = new string(token.Where(c => char.IsLetterOrDigit(c)).ToArray());
                
                // check if empty or stopword
                if (string.IsNullOrEmpty(cleaned) || _stopwords.Contains(cleaned))
                {
                    continue;
                }
                
                // apply stemming
                string stemmed = SimpleStem(cleaned);
                
                // check minimum length
                if (!string.IsNullOrEmpty(stemmed) && stemmed.Length >= 3)
                {
                    processed.Add(stemmed);
                }
            }
            
            return processed;
        }
        
        /// <summary>
        /// simple stemming function that removes common english suffixes
        /// </summary>
        private static string SimpleStem(string word)
        {
            if (string.IsNullOrEmpty(word) || word.Length <= 3)
            {
                return word;
            }
            
            // handle plurals and past tense
            if (word.EndsWith("s") && !word.EndsWith("ss") && !word.EndsWith("us") && word.Length > 3)
            {
                word = word.Substring(0, word.Length - 1);
            }
            else if (word.EndsWith("es") && word.Length > 4)
            {
                word = word.Substring(0, word.Length - 2);
            }
            else if (word.EndsWith("ed") && word.Length > 4)
            {
                word = word.Substring(0, word.Length - 2);
            }
            else if (word.EndsWith("ing") && word.Length > 5)
            {
                word = word.Substring(0, word.Length - 3);
            }
            else if (word.EndsWith("ly") && word.Length > 4)
            {
                word = word.Substring(0, word.Length - 2);
            }
            
            return word;
        }
    }
} 