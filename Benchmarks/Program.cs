using BenchmarkDotNet.Running;
using System;
namespace SearchEngine.Benchmarks;

public class Program
{
    public static void Main(string[] args)
    {
        if (args.Length > 0)
        {
            switch (args[0])
            {
                case "memory-analysis":
                    Console.WriteLine("Running memory usage analysis across all file sizes and exporting to CSV...");
                    var indexBenchmark = new IndexConstructionBenchmark();
                    indexBenchmark.PrintMemoryUsage(exportToCsv: true);
                    break;
                    
                case "benchmark":
                    Console.WriteLine("Running Index Construction Benchmark...");
                    BenchmarkRunner.Run<IndexConstructionBenchmark>();
                    
                    Console.WriteLine("\nRunning Search Operations Benchmark...");
                    BenchmarkRunner.Run<SearchOperationsBenchmark>();
                    break;
                    
                case "compression-stats":
                    Console.WriteLine("Analyzing document compression statistics...");
                    var compBenchmark = new CompressionBenchmark();
                    compBenchmark.PrintCompressionStats();
                    break;
                    
                case "delta-stats":
                    Console.WriteLine("Analyzing delta encoding statistics...");
                    var deltaBenchmark = new CompressionBenchmark();
                    deltaBenchmark.PrintDeltaEncodingStats();
                    break;
                    
                case "compression-benchmark":
                    Console.WriteLine("Running compression performance benchmarks...");
                    BenchmarkRunner.Run<CompressionBenchmark>();
                    break;
                    
                case "filter-analysis":
                    Console.WriteLine("Running filter analysis across all file sizes...");
                    var filterBenchmark = new FilterAnalysisBenchmark();
                    filterBenchmark.RunFilterAnalysis();
                    break;
                    
                default:
                    ShowHelp();
                    break;
            }
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
            
            ShowHelp();
        }
    }
    
    private static void ShowHelp()
    {
        Console.WriteLine("\nAvailable commands:");
        Console.WriteLine("  memory-analysis      - Analyze memory usage across all file sizes and export to CSV");
        Console.WriteLine("  benchmark            - Run full index construction and search operation benchmarks");
        Console.WriteLine("  compression-stats    - Analyze document compression statistics and export to CSV");
        Console.WriteLine("  delta-stats          - Analyze delta encoding statistics and export to CSV");
        Console.WriteLine("  compression-benchmark - Run performance benchmarks for compression and delta encoding");
        Console.WriteLine("  filter-analysis      - Analyze how different filter combinations affect memory usage");
    }
} 