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

    [CsvExporter]
    [MemoryDiagnoser]
    [SimpleJob(warmupCount: 1, iterationCount: 1)]
    public class IndexConstructionBenchmark
    {
        [Params("100KB.txt", "1MB.txt")]
        //[Params("100KB.txt", "1MB.txt", "2MB.txt", "5MB.txt", "10MB.txt", "20MB.txt", "50MB.txt", "100MB.txt")]
        public string FileName { get; set; }

        [Benchmark]
        public Index BenchmarkIndexConstruction()
        {
            string projectDir = "/home/shierfall/RiderProjects/search-engine-project";
            string fullPath = System.IO.Path.Combine(projectDir, FileName);
            return new Index(fullPath);
        }
    }

    [CsvExporter]
    [MemoryDiagnoser]
    [SimpleJob(warmupCount: 5, iterationCount: 40)]
    public class SingleWordQueryBenchmark
    {
        [Params("100KB.txt", "1MB.txt", "2MB.txt", "5MB.txt", "10MB.txt", "20MB.txt", "50MB.txt", "100MB.txt")]
        public string FileName { get; set; }

        [Params("and", "or", "cat", "bread")]
        public string Query { get; set; }

        private Index index;

        [GlobalSetup]
        public void Setup()
        {
            string projectDir = "/home/shierfall/RiderProjects/search-engine-project";
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

    [CsvExporter]
    [MemoryDiagnoser]
    [SimpleJob(warmupCount: 5, iterationCount: 50)]
    public class BoolQueryBenchmark
    {
        [Params("100KB.txt", "1MB.txt", "2MB.txt", "5MB.txt", "10MB.txt", "20MB.txt", "50MB.txt", "100MB.txt")]
        public string FileName { get; set; }

        [Params("and && or", "and || or", "cat && dog", "bread || apple")]
        public string QueryBool { get; set; }

        private Index index;

        [GlobalSetup]
        public void Setup()
        {
            string projectDir = "/home/shierfall/RiderProjects/search-engine-project";
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

    // Combined benchmark for testing all search methods with multiple queries.
    [CsvExporter]
    [MemoryDiagnoser]
    [SimpleJob(warmupCount: 5, iterationCount: 10)]
    public class AllSearchMethodsBenchmark
    {
        [Params("100KB.txt", "1MB.txt", "2MB.txt", "5MB.txt", "10MB.txt", "20MB.txt", "50MB.txt", "100MB.txt")]
        public string FileName { get; set; }
        
        [Params("and", "or", "cat", "bread")]
        public string SingleQuery { get; set; }
        

        [Params("ca", "do", "br", "ap")]
        public string PrefixQuery { get; set; }


        [Params("in the", "of the", "to the", "on the")]
        public string PhraseQuery { get; set; }

        // Multiple boolean queries.
        [Params("cat && dog", "and || or", "cat && apple", "learn || bread || algorithm")]
        public string BoolQuery { get; set; }

        private Index index;

        [GlobalSetup]
        public void Setup()
        {
            string projectDir = "/home/shierfall/RiderProjects/search-engine-project";
            string fullPath = System.IO.Path.Combine(projectDir, FileName);
            index = new Index(fullPath);
        }

        // Trie-based exact search.
        [Benchmark]
        public void BenchmarkSearchTrie()
        {
            index.SearchTrie(SingleQuery);
        }

        // Trie-based auto-complete.
        [Benchmark]
        public void BenchmarkAutoComplete()
        {
            index.AutoComplete(PrefixQuery);
        }

        // Trie-based prefix search.
        [Benchmark]
        public void BenchmarkPrefixSearchTrie()
        {
            index.PrefixSearchTrie(PrefixQuery);
        }

        // Trie-based phrase search.
        [Benchmark]
        public void BenchmarkPhraseSearchTrie()
        {
            index.PhraseSearchTrie(PhraseQuery);
        }

        // Inverted index exact search.
        [Benchmark]
        public void BenchmarkSearchIndex()
        {
            index.SearchIndex(SingleQuery);
        }

        // Inverted index prefix search.
        [Benchmark]
        public void BenchmarkPrefixSearchIndex()
        {
            index.PrefixSearchIndex(PrefixQuery);
        }

        // Inverted index phrase search.
        [Benchmark]
        public void BenchmarkPhraseSearchIndex()
        {
            index.PhraseSearchIndex(PhraseQuery);
        }

        // Inverted index ranked search.
        [Benchmark]
        public void BenchmarkSearchRankedIndex()
        {
            index.SearchRankedIndex(SingleQuery);
        }

        // Boolean search using inverted index (naive).
        [Benchmark]
        public void BenchmarkBooleanSearchNaiveIndex()
        {
            index.BooleanSearchNaiveIndex(BoolQuery);
        }

        // Boolean search using inverted index (bitset).
        [Benchmark]
        public void BenchmarkBooleanSearchBitsetIndex()
        {
            index.BooleanSearchBitsetIndex(BoolQuery);
        }

        // Boolean search using trie (naive).
        [Benchmark]
        public void BenchmarkBooleanSearchNaiveTrie()
        {
            index.BooleanSearchNaiveTrie(BoolQuery);
        }

        // Boolean search using trie (bitset).
        [Benchmark]
        public void BenchmarkBooleanSearchBitsetTrie()
        {
            index.BooleanSearchBitsetTrie(BoolQuery);
        }

        // Trie-based ranked search.
        [Benchmark]
        public void BenchmarkRankedSearchTrie()
        {
            index.RankedSearchTrie(SingleQuery);
        }
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            // Uncomment the benchmark(s) you wish to run:
            //BenchmarkRunner.Run<IndexConstructionBenchmark>();
            //BenchmarkRunner.Run<SingleWordQueryBenchmark>();
            //BenchmarkRunner.Run<BoolQueryBenchmark>();
            BenchmarkRunner.Run<AllSearchMethodsBenchmark>();
        }
    }
}

/*
Alternative main (interactive mode):

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

        while (true)
        {
            Console.WriteLine("Input search string or type exit to stop");
            string searchStr = Console.ReadLine();

            if (searchStr.Equals("exit", StringComparison.OrdinalIgnoreCase))
            {
                break;
            }

            index.SearchIndex(searchStr);
        }
    }
}
*/
