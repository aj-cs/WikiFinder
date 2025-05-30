using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using SearchEngine.Analysis;
using SearchEngine.Core;
using SearchEngine.Core.Interfaces;

namespace ManualBenchmarks
{
    public class ManualSearchBenchmark
    {
        private static readonly string[] FileSizes = { "100KB", "1MB", "2MB", "5MB", "10MB" };
        private static readonly string BasePath = "/home/shierfall/Downloads/texts/"; // Adjust as needed
        private static readonly int Iterations = 15; // Adjustable

        private static readonly Dictionary<string, List<string>> QueryCategories = new()
        {
            ["Exact"] = new() { "and", "or", "cat", "bread" },
            ["Prefix"] = new() { "an*", "or*", "ca*", "br*" },
            ["Phrase"] = new() { "he was", "it was a", "and when", "or when" },
            ["Boolean"] = new() { "and && or || cat", "cat && bread", "bread && cat || or", "or && and" }
        };

        public static void Main(string[] args)
        {
            var csvPath = "ManualBenchmarks/search_benchmark_results.csv";
            using var writer = new StreamWriter(csvPath, false, System.Text.Encoding.UTF8);
            writer.WriteLine("filesize,searchtype,query,time_ms");

            foreach (var fileSize in FileSizes)
            {
                var filePath = Path.Combine(BasePath, $"test_{fileSize}.txt");
                if (!File.Exists(filePath))
                {
                    Console.WriteLine($"File not found: {filePath}");
                    continue;
                }
                var content = File.ReadAllText(filePath);
                var analyzer = new Analyzer();
                var tokens = analyzer.Analyze(content).ToList();

                // Index for Trie
                var trie = new CompactTrieIndex();
                trie.SetBM25Enabled(false);
                foreach (var (token, idx) in tokens.Select((t, i) => (t, i)))
                {
                    trie.AddDocument(idx, new[] { token });
                }

                // Index for InvertedIndex
                var inverted = new InvertedIndex();
                inverted.SetBM25Enabled(false);
                foreach (var (token, idx) in tokens.Select((t, i) => (t, i)))
                {
                    inverted.AddDocument(idx, new[] { token });
                }

                foreach (var (searchType, queries) in QueryCategories)
                {
                    foreach (var query in queries)
                    {
                        // Trie
                        double totalTimeTrie = 0;
                        for (int i = 0; i < Iterations; i++)
                        {
                            var sw = Stopwatch.StartNew();
                            switch (searchType)
                            {
                                case "Exact":
                                    trie.ExactSearch(query);
                                    break;
                                case "Prefix":
                                    trie.PrefixSearch(query.TrimEnd('*'));
                                    break;
                                case "Phrase":
                                    trie.PhraseSearch(query);
                                    break;
                                case "Boolean":
                                    trie.BooleanSearch(query);
                                    break;
                            }
                            sw.Stop();
                            totalTimeTrie += sw.Elapsed.TotalMilliseconds;
                        }
                        double meanTrie = totalTimeTrie / Iterations;
                        writer.WriteLine($"{fileSize},Trie,{query},{meanTrie.ToString(CultureInfo.InvariantCulture)}");

                        // Inverted Index
                        double totalTimeInv = 0;
                        for (int i = 0; i < Iterations; i++)
                        {
                            var sw = Stopwatch.StartNew();
                            switch (searchType)
                            {
                                case "Exact":
                                    inverted.ExactSearch(query);
                                    break;
                                case "Prefix":
                                    inverted.PrefixSearch(query.TrimEnd('*'));
                                    break;
                                case "Phrase":
                                    inverted.PhraseSearch(query);
                                    break;
                                case "Boolean":
                                    inverted.BooleanSearch(query);
                                    break;
                            }
                            sw.Stop();
                            totalTimeInv += sw.Elapsed.TotalMilliseconds;
                        }
                        double meanInv = totalTimeInv / Iterations;
                        writer.WriteLine($"{fileSize},InvertedIndex,{query},{meanInv.ToString(CultureInfo.InvariantCulture)}");
                    }
                }
                writer.Flush();
                Console.WriteLine($"Benchmarked file size: {fileSize}");
            }
            Console.WriteLine($"Benchmark complete. Results written to {csvPath}");
        }
    }
}
