using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using SearchEngine.Analysis;
using SearchEngine.Core;
using SearchEngine.Core.Interfaces;
using SearchEngine.Analysis.Tokenizers;
using System.Text;

namespace SearchEngine.Benchmarks;

[MemoryDiagnoser]
[WarmupCount(3)]
[IterationCount(20)]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CsvExporter]
public class SearchOperationsBenchmark
{
    private string[] _fileSizes = new[] { "100KB", "1MB", "2MB", "5MB", "10MB", "20MB", "50MB", "100MB", "200MB", "400MB" };
    private string _basePath = "/home/shierfall/Downloads/texts/"; // path to text files
    private Analyzer _analyzer = null!;
    private IExactPrefixIndex _trie = null!;
    private IExactPrefixIndex _simpleInvertedIndex = null!;
    private IBloomFilter _bloomFilter = null!;
    private string _currentFile = null!;
    private string _currentContent = null!;
    
    // categorize queries by type for more meaningful benchmarks
    private static readonly Dictionary<string, List<string>> _queryCategories = new()
    {
        ["Exact"] = new() { "and", "or", "cat", "bread" },
        ["Prefix"] = new() { "an*", "or*", "ca*", "br*" },
        ["Phrase"] = new() { "he was", "it was a", "and when", "or when" },
        ["Boolean"] = new() { "and && or || cat", "cat && bread", "bread && cat || or", "or && and" }
    };

    [ParamsSource(nameof(FileSizes))]
    public string FileSize { get; set; } = null!;

    public IEnumerable<string> FileSizes => _fileSizes;

    [GlobalSetup]
    public void Setup()
    {
        _analyzer = new Analyzer(new MinimalTokenizer());
        _trie = new CompactTrieIndex();
        _simpleInvertedIndex = new SimpleInvertedIndex();
        _bloomFilter = new BloomFilter(2000000, 0.03);
        _currentFile = Path.Combine(_basePath, $"{FileSize}.txt");
        
        Console.WriteLine($"Loading file: {_currentFile}");
        _currentContent = File.ReadAllText(_currentFile);
        Console.WriteLine($"File loaded: {_currentContent.Length} characters");

        // index the content
        var tokens = _analyzer.Analyze(_currentContent).ToList();
        Console.WriteLine($"Tokens generated: {tokens.Count}");
        
        _trie.AddDocument(1, tokens);
        _simpleInvertedIndex.AddDocument(1, tokens);
        
        foreach (var token in tokens)
        {
            _bloomFilter.Add(token.Term);
        }
        
        Console.WriteLine("Benchmark setup complete.");
    }

    // exact search benchmarks
    [BenchmarkCategory("ExactSearch")]
    [Arguments("and")]
    [Arguments("or")]
    [Arguments("cat")]
    [Arguments("bread")]
    [Benchmark(Description = "Trie")]
    public bool TrieExact(string query)
    {
        return _trie.Search(query);
    }

    [BenchmarkCategory("ExactSearch")]
    [Arguments("and")]
    [Arguments("or")]
    [Arguments("cat")]
    [Arguments("bread")]
    [Benchmark(Description = "InvertedIndex")]
    public bool InvertedIndexExact(string query)
    {
        return _simpleInvertedIndex.Search(query);
    }

    [BenchmarkCategory("ExactSearch")]
    [Arguments("and")]
    [Arguments("or")]
    [Arguments("cat")]
    [Arguments("bread")]
    [Benchmark(Description = "BloomFilter")]
    public bool BloomFilterExact(string query)
    {
        return _bloomFilter.MightContain(query);
    }

    // prefix search benchmarks - documents only
    [BenchmarkCategory("PrefixSearch")]
    [Arguments("an")]
    [Arguments("or")]
    [Arguments("ca")]
    [Arguments("br")]
    [Benchmark(Description = "Trie")]
    public List<int> TriePrefix(string query)
    {
        return _trie.PrefixSearchDocuments(query);
    }

    [BenchmarkCategory("PrefixSearch")]
    [Arguments("an")]
    [Arguments("or")]
    [Arguments("ca")]
    [Arguments("br")]
    [Benchmark(Description = "InvertedIndex")]
    public List<int> InvertedIndexPrefix(string query)
    {
        return _simpleInvertedIndex.PrefixSearchDocuments(query);
    }

    // phrase search benchmarks
    [BenchmarkCategory("PhraseSearch")]
    [Arguments("and or")]
    [Arguments("cat bread")]
    [Arguments("bread cat")]
    [Arguments("or and")]
    [Benchmark(Description = "InvertedIndex")]
    public List<(int docId, int count)> InvertedIndexPhrase(string query)
    {
        // cast to SimpleInvertedIndex to access its PhraseSearch method
        if (_simpleInvertedIndex is SimpleInvertedIndex simpleIdx)
        {
            return simpleIdx.PhraseSearch(query);
        }
        return new List<(int docId, int count)>();
    }

    // boolean search benchmarks
    [BenchmarkCategory("BooleanSearch")]
    [Arguments("and && or || cat")]
    [Arguments("cat && bread")]
    [Arguments("bread && cat || or")]
    [Arguments("or && and")]
    [Benchmark(Description = "Trie")]
    public List<(int docId, int count)> TrieBoolean(string query)
    {
        return _trie.BooleanSearchNaive(query);
    }

    [BenchmarkCategory("BooleanSearch")]
    [Arguments("and && or || cat")]
    [Arguments("cat && bread")]
    [Arguments("bread && cat || or")]
    [Arguments("or && and")]
    [Benchmark(Description = "InvertedIndex-Naive")]
    public List<(int docId, int count)> InvertedIndexBooleanNaive(string query)
    {
        return _simpleInvertedIndex.BooleanSearchNaive(query);
    }

    [BenchmarkCategory("BooleanSearch")]
    [Arguments("and && or || cat")]
    [Arguments("cat && bread")]
    [Arguments("bread && cat || or")]
    [Arguments("or && and")]
    [Benchmark(Description = "InvertedIndex-Bitset")]
    public List<(int docId, int count)> InvertedIndexBooleanBitset(string query)
    {
        // cast to SimpleInvertedIndex to use its bitset-based boolean search
        if (_simpleInvertedIndex is SimpleInvertedIndex simpleIdx)
        {
            return simpleIdx.BooleanSearchNaive(query);
        }
        return _simpleInvertedIndex.BooleanSearchNaive(query);
    }
} 