using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Running;

namespace SearchEngineProject;

[CsvExporter]
[MemoryDiagnoser]
public class IndexBenchmark
{
    // Parameterized input: run benchmarks for each file in this list.
    // WestburyLab.wikicorp.201004_100KB.txt
    // WestburyLab.wikicorp.201004_100MB.txt
    // WestburyLab.wikicorp.201004_10MB.txt
    // WestburyLab.wikicorp.201004_1MB.txt
    // WestburyLab.wikicorp.201004_200MB.txt
    // WestburyLab.wikicorp.201004_20MB.txt
    // WestburyLab.wikicorp.201004_2MB.txt
    // WestburyLab.wikicorp.201004_400MB.txt
    // WestburyLab.wikicorp.201004_50MB.txt
    // WestburyLab.wikicorp.201004_5MB.txt
    // WestburyLab.wikicorp.201004_800MB.txt
    // WestburyLab.wikicorp.201004.txt
    [Params("WestburyLab.wikicorp.201004_100KB.txt",
            "WestburyLab.wikicorp.201004_1MB.txt",
            "WestburyLab.wikicorp.201004_2MB.txt",
            "WestburyLab.wikicorp.201004_5MB.txt",
            "WestburyLab.wikicorp.201004_10MB.txt",
            "WestburyLab.wikicorp.201004_20MB.txt"
            // "WestburyLab.wikicorp.201004_50MB.txt",
            // "WestburyLab.wikicorp.201004_200MB.txt",
            //"WestburyLab.wikicorp.201004_400MB.txt"
            )]
    public string FileName { get; set; }

    // The set of queries for search benchmarks.
    private readonly string[] queries = new[] { "and", "or", "cat", "bread" };

    // We'll use this index for the search benchmarks.
    private Index index;

    // GlobalSetup for search benchmarks; this setup is not measured.
    // It runs before benchmarks that use the index.
    [GlobalSetup(Targets = new[] { nameof(BenchmarkPrefixSearch), nameof(BenchmarkPrefixSearchDocuments) })]
    public void SetupSearch()
    {
        // Use the absolute path to your project directory
        string projectDir = "/home/arjun/Documents/SearchEngine/SearchEngineProject";
        string fullPath = System.IO.Path.Combine(projectDir, FileName);
        index = new Index(fullPath);
    }

    // Benchmark for measuring preprocessing (index construction) time.

    [Benchmark]
    public Index BenchmarkIndexConstruction()
    {
        string projectDir = "/home/arjun/Documents/SearchEngine/SearchEngineProject";
        string fullPath = System.IO.Path.Combine(projectDir, FileName);
        return new Index(fullPath);
    }


    // Benchmark for the PrefixSearch method.
    [Benchmark]
    public void BenchmarkPrefixSearch()
    {
        foreach (var query in queries)
        {
            index.PrefixSearch(query);
        }
    }

    // Benchmark for the PrefixSearchDocuments method.
    [Benchmark]
    public void BenchmarkPrefixSearchDocuments()
    {
        foreach (var query in queries)
        {
            index.PrefixSearchDocuments(query);
        }
    }
}
public class Program
{
    public static void Main(string[] args)
    {
        BenchmarkRunner.Run<IndexBenchmark>();
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

