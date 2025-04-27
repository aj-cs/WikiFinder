using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using System.Diagnostics;

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

public class IndexConfig : ManualConfig
{
    public IndexConfig()
    {
        AddJob(Job.Default
                .WithWarmupCount(1)
                .WithIterationCount(1)
                .WithInvocationCount(1)
                .WithUnrollFactor(1)
                .WithId("IndexConstructionJob"));
    }
}
public class QueryConfig : ManualConfig
{
    public QueryConfig()
    {
        AddJob(Job.Default
                .WithWarmupCount(1)
                .WithIterationCount(3)
                .WithInvocationCount(1)
                .WithUnrollFactor(1)
                .WithId("QueryJob"));
    }
}
[CsvExporter]
[MemoryDiagnoser]
public class IndexConstructionBenchmark
{
    // Parameterized list of file names.
    // (These file names are relative to your project directory.)
    [Params("100KB.txt", "1MB.txt", "2MB.txt", "5MB.txt", "10MB.txt", "20MB.txt", "50MB.txt", "100MB.txt")]
    public string FileName { get; set; }

    [Benchmark]
    public Index BenchmarkIndexConstruction()
    {
        // Use an absolute path to your project directory
        string projectDir = "/zhome/79/1/188120/search-engine-project";
        string fullPath = System.IO.Path.Combine(projectDir, FileName);
        return new Index(fullPath);
    }
}

// This benchmark class measures the time and memory of search queries individually.
[CsvExporter]
[MemoryDiagnoser]
public class QueryBenchmark
{
    // Parameterized file name for building the index.
    [Params("100KB.txt", "1MB.txt", "2MB.txt", "5MB.txt", "10MB.txt", "20MB.txt", "50MB.txt", "100MB.txt")]
    public string FileName { get; set; }

    // Parameterized query so that each query ("and", "or", "cat", "bread") is benchmarked separately.
    [Params("and", "or", "cat", "bread")]
    public string Query { get; set; }

    private Index index;

    [GlobalSetup]
    public void Setup()
    {
        string projectDir = "/zhome/79/1/188120/search-engine-project";
        string fullPath = System.IO.Path.Combine(projectDir, FileName);
        index = new Index(fullPath);
    }

    // Measures the time for the PrefixSearch method for a single query.
    [Benchmark]
    public void BenchmarkPrefixSearch()
    {
        index.PrefixSearch(Query);
    }

    // Measures the time for the PrefixSearchDocuments method for a single query.
    [Benchmark]
    public void BenchmarkPrefixSearchDocuments()
    {
        index.PrefixSearchDocuments(Query);
    }

    //Measures the time for the Normal Search
    [Benchmark]
    public void BenchmarkNormalSearchDocuments()
    {
        index.PrefixSearchDocuments(Query);
    }
}

// The Program class runs both sets of benchmarks.
public class Program
{
    public static void Main(string[] args)
    {
        // Run the index construction benchmarks.
        // BenchmarkRunner.Run<IndexConstructionBenchmark>();

        // Run the query benchmarks
        // BenchmarkRunner.Run<QueryBenchmark>();
        if (args.Length == 0)
        {
            Console.WriteLine("Usage: Index1 <filename>");
            return;
        }
        var stopwatch = new Stopwatch();
        Console.WriteLine("Preprocessing " + args[0]);
        stopwatch.Start();
        Index index = new Index(args[0]);
        stopwatch.Stop();
        Console.WriteLine($"Pre-process time(s) {stopwatch.ElapsedMilliseconds / 1000}");

        while (true)
        {
            Console.WriteLine("Input search string or type exit to stop");
            string searchStr = Console.ReadLine();

            if (searchStr.Equals("exit", StringComparison.OrdinalIgnoreCase))
            {
                break;
            }

            index.PrintDemo(searchStr, searchStr);
        }
    }
}
/*class Program
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

            index.PrefixSearchDocuments(searchStr);
            Console.WriteLine($"\nAuto-completion of words starting with '{searchStr}': ");
            index.PrefixSearch(searchStr);
        }
    }
    
}*/

