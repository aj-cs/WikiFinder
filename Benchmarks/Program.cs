using BenchmarkDotNet.Running;
using System;
namespace SearchEngine.Benchmarks;

public class Program
{
    public static void Main(string[] args)
    {
        // run construction benchmark
        //Console.WriteLine("Running Index Construction Benchmark...");
        //BenchmarkRunner.Run<IndexConstructionBenchmark>();

        // run search operations benchmark
        //Console.WriteLine("\nRunning Search Operations Benchmark...");
        //BenchmarkRunner.Run<SearchOperationsBenchmark>();            
        if (args.Length > 0 && args[0] == "memory-analysis")
        {
            Console.WriteLine("Running memory usage analysis across all file sizes and exporting to CSV...");
            IndexConstructionBenchmark benchmark = new IndexConstructionBenchmark();

            benchmark.PrintMemoryUsage(exportToCsv: true);
        }
        else if (args.Length > 0 && args[0] == "benchmark")
        {
            Console.WriteLine("Running Index Construction Benchmark...");
            BenchmarkRunner.Run<IndexConstructionBenchmark>();
            
            Console.WriteLine("\nRunning Search Operations Benchmark...");
            BenchmarkRunner.Run<SearchOperationsBenchmark>();
        }
        else
        {
            // Default behavior - run memory usage for a single file size
            IndexConstructionBenchmark benchmark = new IndexConstructionBenchmark();
            benchmark.FileSize = "10MB"; // Default file size
            benchmark.Setup();
            benchmark.TrieConstruction();
            benchmark.InvertedIndexConstruction();
            benchmark.BloomFilterConstruction();
            benchmark.PrintMemoryUsage();
            
            Console.WriteLine("\nRun with 'memory-analysis' argument to analyze all file sizes and export to CSV.");
            Console.WriteLine("Run with 'benchmark' argument to run full benchmarks.");
        }
    }
} 