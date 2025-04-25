using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;

namespace SearchEngineProject
{
    /*
     * 100KB.txt
     * 100MB.txt
     * 10MB.txt
     * 1GB.txt
     * 1MB.txt
     * 200MB.txt
     * 20MB.txt
     * 2MB.txt
     * 400MB.txt
     * 50MB.txt
     * 5MB.txt
     * 800MB.txt
     */

    #region Index Construction Benchmark

    [CsvExporter]
    [MemoryDiagnoser]
    [SimpleJob(warmupCount: 1, iterationCount: 1)]
    public class IndexConstructionBenchmark
    {
        [Params("100KB.txt", "1MB.txt")]
        //[Params("100KB.txt", "1MB.txt", "2MB.txt", "5MB.txt", "10MB.txt", "20MB.txt", "50MB.txt", "100MB.txt")]
        public string FileName { get; set; } = string.Empty;

        [Benchmark]
        public Index BenchmarkIndexConstruction()
        {
            string projectDir = "/zhome/6b/1/188023/search-engine-project";
            string fullPath = System.IO.Path.Combine(projectDir, FileName);
            return new Index(fullPath);
        }
    }

    #endregion

    #region Single Word Query Benchmark (Exact Match, Trie Only)

    [CsvExporter]
    [MemoryDiagnoser]
    [SimpleJob(warmupCount: 5, iterationCount: 40)]
    public class SingleWordQueryBenchmark
    {
        [Params("100KB.txt", "1MB.txt", "2MB.txt", "5MB.txt", "10MB.txt", "20MB.txt", "50MB.txt", "100MB.txt")]
        public string FileName { get; set; } = string.Empty;

        [Params("and", "or", "cat", "bread")] public string Query { get; set; } = string.Empty;

        private Index index = null!;

        [GlobalSetup]
        public void Setup()
        {
            string projectDir = "/zhome/6b/1/188023/search-engine-project";
            string fullPath = System.IO.Path.Combine(projectDir, FileName);
            index = new Index(fullPath);
        }

        [Benchmark]
        public void BenchmarkPrefixSearchTrie()
        {
            index.PrefixSearchTrie(Query);
        }

        [Benchmark]
        public void BenchmarkNormalSearchDocumentsTrie()
        {
            index.SearchTrie(Query);
        }

        [Benchmark]
        public void BenchmarkRankingSearchTrie()
        {
            index.RankedSearchTrie(Query);
        }
    }

    #endregion

    #region Boolean Query Benchmark (Trie and Inverted Index)

    [CsvExporter]
    [MemoryDiagnoser]
    [SimpleJob(warmupCount: 5, iterationCount: 50)]
    public class BoolQueryBenchmark
    {
        [Params("100KB.txt", "1MB.txt", "2MB.txt", "5MB.txt", "10MB.txt", "20MB.txt", "50MB.txt", "100MB.txt")]
        public string FileName { get; set; } = string.Empty;

        [Params("and && or", "and || or", "cat && dog", "bread || apple")]
        public string QueryBool { get; set; } = string.Empty;

        private Index index = null!;

        [GlobalSetup]
        public void Setup()
        {
            string projectDir = "/zhome/6b/1/188023/search-engine-project";
            string fullPath = System.IO.Path.Combine(projectDir, FileName);
            index = new Index(fullPath);
        }

        [Benchmark]
        public void BenchmarkBoolQueryTrieNaive()
        {
            index.BooleanSearchNaiveTrie(QueryBool);
        }

        [Benchmark]
        public void BenchmarkBoolQueryTrie()
        {
            index.BooleanSearchBitsetTrie(QueryBool);
        }

        [Benchmark]
        public void BenchmarkBoolQueryIndexNaive()
        {
            index.BooleanSearchNaiveIndex(QueryBool);
        }

        [Benchmark]
        public void BenchmarkBoolQueryIndex()
        {
            index.BooleanSearchBitsetIndex(QueryBool);
        }
    }

    #endregion

    #region New Benchmarks for Each Separate Search Type

    // Exact Search Benchmark (Exact match using trie vs. inverted index)
    [CsvExporter]
    [MemoryDiagnoser]
    [SimpleJob(warmupCount: 5, iterationCount: 50)]
    public class ExactSearchBenchmark
    {
        [Params("100KB.txt", "1MB.txt", "2MB.txt", "5MB.txt", "10MB.txt", "20MB.txt", "50MB.txt", "100MB.txt")]
        public string FileName { get; set; } = string.Empty;

        [Params("cat", "dog", "bread", "apple")]
        public string Query { get; set; } = string.Empty;

        private Index index = null!;

        [GlobalSetup]
        public void Setup()
        {
            string projectDir = "/zhome/6b/1/188023/search-engine-project";
            string fullPath = System.IO.Path.Combine(projectDir, FileName);
            index = new Index(fullPath);
        }

        [Benchmark]
        public void TrieExactSearch() => index.SearchTrie(Query);

        [Benchmark]
        public void InvertedIndexExactSearch() => index.SearchIndex(Query);
    }


    // Prefix Search Benchmark (Trie prefix search, inverted index prefix search, and auto-complete)

    [CsvExporter]
    [MemoryDiagnoser]
    [SimpleJob(warmupCount: 5, iterationCount: 50)]
    public class PrefixSearchBenchmark
    {
        [Params("100KB.txt", "1MB.txt", "2MB.txt", "5MB.txt", "10MB.txt", "20MB.txt", "50MB.txt", "100MB.txt")]
        public string FileName { get; set; } = string.Empty;

        [Params("ca", "do", "br", "ap")] public string Prefix { get; set; } = string.Empty;

        private Index index = null!;

        [GlobalSetup]
        public void Setup()
        {
            string projectDir = "/zhome/6b/1/188023/search-engine-project";
            string fullPath = System.IO.Path.Combine(projectDir, FileName);
            index = new Index(fullPath);
        }

        [Benchmark]
        public void TriePrefixSearch() => index.PrefixSearchTrie(Prefix);

        [Benchmark]
        public void InvertedIndexPrefixSearch() => index.PrefixSearchIndex(Prefix);

        [Benchmark]
        public void AutoCompleteSearch() => index.AutoComplete(Prefix);
    }

    // Phrase Search Benchmark (Phrase search using trie vs. inverted index)
    [CsvExporter]
    [MemoryDiagnoser]
    [SimpleJob(warmupCount: 5, iterationCount: 50)]
    public class PhraseSearchBenchmark
    {
        [Params("100KB.txt", "1MB.txt", "2MB.txt", "5MB.txt", "10MB.txt", "20MB.txt", "50MB.txt", "100MB.txt")]
        public string FileName { get; set; } = string.Empty;

        // Common two-word phrases likely to appear frequently.
        [Params("in the", "of the", "to the", "on the")]
        public string Phrase { get; set; } = string.Empty;

        private Index index = null!;

        [GlobalSetup]
        public void Setup()
        {
            string projectDir = "/zhome/6b/1/188023/search-engine-project";
            string fullPath = System.IO.Path.Combine(projectDir, FileName);
            index = new Index(fullPath);
        }

        [Benchmark]
        public void TriePhraseSearch() => index.PhraseSearchTrie(Phrase);

        [Benchmark]
        public void InvertedIndexPhraseSearch() => index.PhraseSearchIndex(Phrase);
    }


    // Ranked Search Benchmark (Ranked search using trie vs. inverted index)

    [CsvExporter]
    [MemoryDiagnoser]
    [SimpleJob(warmupCount: 5, iterationCount: 50)]
    public class RankedSearchBenchmark
    {
        [Params("100KB.txt", "1MB.txt", "2MB.txt", "5MB.txt", "10MB.txt", "20MB.txt", "50MB.txt", "100MB.txt")]
        public string FileName { get; set; } = string.Empty;

        [Params("cat", "dog", "bread", "apple")]
        public string Query { get; set; } = string.Empty;

        private Index index = null!;

        [GlobalSetup]
        public void Setup()
        {
            string projectDir = "/zhome/6b/1/188023/search-engine-project";
            string fullPath = System.IO.Path.Combine(projectDir, FileName);
            index = new Index(fullPath);
        }

        [Benchmark]
        public void TrieRankedSearch() => index.RankedSearchTrie(Query);

        [Benchmark]
        public void InvertedIndexRankedSearch() => index.SearchRankedIndex(Query);
    }


    // Boolean Search Benchmark (Boolean search using trie and inverted index implementations)

    [CsvExporter]
    [MemoryDiagnoser]
    [SimpleJob(warmupCount: 5, iterationCount: 50)]
    public class BooleanSearchBenchmark
    {
        [Params("100KB.txt", "1MB.txt", "2MB.txt", "5MB.txt", "10MB.txt", "20MB.txt", "50MB.txt", "100MB.txt")]
        public string FileName { get; set; } = string.Empty;

        [Params("cat && dog", "bread || apple", "cat && apple", "dog || bread")]
        public string BoolQuery { get; set; } = string.Empty;

        private Index index = null!;

        [GlobalSetup]
        public void Setup()
        {
            string projectDir = "/zhome/6b/1/188023/search-engine-project";
            string fullPath = System.IO.Path.Combine(projectDir, FileName);
            index = new Index(fullPath);
        }

        [Benchmark]
        public void BooleanSearchNaiveTrie() => index.BooleanSearchNaiveTrie(BoolQuery);

        [Benchmark]
        public void BooleanSearchBitsetTrie() => index.BooleanSearchBitsetTrie(BoolQuery);

        [Benchmark]
        public void BooleanSearchNaiveIndex() => index.BooleanSearchNaiveIndex(BoolQuery);

        [Benchmark]
        public void BooleanSearchBitsetIndex() => index.BooleanSearchBitsetIndex(BoolQuery);
    }

    
    
    [CsvExporter]
    [MemoryDiagnoser]
    [SimpleJob(warmupCount: 5, iterationCount: 50)]
    public class BloomSearch
    {
        [Params("100KB.txt", "1MB.txt", "2MB.txt", "5MB.txt", "10MB.txt", "20MB.txt", "50MB.txt", "100MB.txt")]
        public string FileName { get; set; } = string.Empty;

        [Params("and", "or", "cat", "bread")]
        public string Query { get; set; } = string.Empty;

        private Index index = null!;

        [GlobalSetup]
        public void Setup()
        {
            string projectDir = "/zhome/6b/1/188023/search-engine-project";
            string fullPath = System.IO.Path.Combine(projectDir, FileName);
            index = new Index(fullPath);
        }

        [Benchmark]
        public void BloomFilterSearch() => index.MayContain(Query);

        [Benchmark]
        public void InvertISearch() => index.SearchIndex(Query);
    }
    #endregion

    #region Program Entry Point

    public class Program
    {
        public static void Main(string[] args)
        {
            // Uncomment the benchmark(s) you wish to run:
            // BenchmarkRunner.Run<IndexConstructionBenchmark>();
           // BenchmarkRunner.Run<SingleWordQueryBenchmark>();
            //BenchmarkRunner.Run<BoolQueryBenchmark>();
            //BenchmarkRunner.Run<ExactSearchBenchmark>();
            //BenchmarkRunner.Run<PrefixSearchBenchmark>();
            //BenchmarkRunner.Run<PhraseSearchBenchmark>();
            //BenchmarkRunner.Run<RankedSearchBenchmark>();
            //BenchmarkRunner.Run<BooleanSearchBenchmark>();
            BenchmarkRunner.Run<BloomSearch>();
        }
    }

    #endregion
}

/*
class Program
{
    static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Usage: Index1 <filename>");
            return;
        }

        Console.WriteLine("Preprocessing " + args[0]);
        Index index = new Index(args[0]);

        index.CompareDataStructures();

        index.AddPublicationFromWikipediaAsync("Birds").Wait();
        Console.WriteLine(index.getTotalUniqueWordCount());
        while (true)
        {
            Console.WriteLine("Input search string or type exit to stop");
            string searchStr = Console.ReadLine();

            if (searchStr.Equals("exit", StringComparison.OrdinalIgnoreCase))
            {
                break;
            }

            var results = index.Search(searchStr);
            foreach (var result in results)
            {
                Console.WriteLine(result);
            }

        }
    }
}

}
*/