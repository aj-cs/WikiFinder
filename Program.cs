using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;

namespace SearchEngineProject;

/*
 * 100KB.txt
100MB.txt
10MB.txt
1GB.txt
1MB.txt
200MB.txt
20MB.txt
2MB.txt
400MB.txt
50MB.txt
5MB.txt
800MB.txt*/


[CsvExporter]
[MemoryDiagnoser]
[SimpleJob(warmupCount: 1, iterationCount: 1)]
public class IndexConstructionBenchmark
{
    [Params("100KB.txt", "1MB.txt", "2MB.txt", "5MB.txt", "10MB.txt", "20MB.txt", "50MB.txt", "100MB.txt")]
    public string FileName { get; set; }

    [Benchmark]
    public Index BenchmarkIndexConstruction()
    {
        string projectDir = "/zhome/6b/1/188023/search-engine-project";
        string fullPath = System.IO.Path.Combine(projectDir, FileName);
        return new Index(fullPath);
    }
}

[CsvExporter]
[MemoryDiagnoser]
[SimpleJob(warmupCount: 5, iterationCount: 1)]
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
        string projectDir = "/zhome/6b/1/188023/search-engine-project";
        string fullPath = System.IO.Path.Combine(projectDir, FileName);
        index = new Index(fullPath);
    }

    [Benchmark]
    public void BenchmarkPrefixSearchIndex()
    {
        index.PrefixSearchIndex(Query);
    }

    [Benchmark]
    public void BenchmarkNormalSearchDocuments()
    {
        index.SearchIndex(Query);
    }

    [Benchmark]
    public void BenchmarkRankingSearch()
    {
        index.SearchRankedIndex(Query);
    }
}

[CsvExporter]
[MemoryDiagnoser]
[SimpleJob(warmupCount: 1, iterationCount: 1)]
public class PhraseQueryBenchmark
{
    [Params("100KB.txt", "1MB.txt", "2MB.txt", "5MB.txt", "10MB.txt", "20MB.txt", "50MB.txt", "100MB.txt")]
    public string FileName { get; set; }

    [Params("and they", "and they were", "it was not", "they could not")]
    public string QueryPhrase { get; set; }

    private Index index;

    [GlobalSetup]
    public void Setup()
    {
        string projectDir = "/zhome/6b/1/188023/search-engine-project";
        string fullPath = System.IO.Path.Combine(projectDir, FileName);
        index = new Index(fullPath);
    }

    [Benchmark]
    public void BenchmarkPhraseSearch()
    {
        index.PhraseSearchIndex(QueryPhrase);
    }
}

public class Program
{
    public static void Main(string[] args)
    {
        BenchmarkRunner.Run<IndexConstructionBenchmark>();
        BenchmarkRunner.Run<SingleWordQueryBenchmark>();
        BenchmarkRunner.Run<PhraseQueryBenchmark>();
    }
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

        while (true)
        {
            Console.WriteLine("Input search string or type exit to stop");
            string searchStr = Console.ReadLine();

            if (searchStr.Equals("exit", StringComparison.OrdinalIgnoreCase))
            {
                break;
            }

            index.PhraseSearchIndex(searchStr);
        }
    }
    
}
*/