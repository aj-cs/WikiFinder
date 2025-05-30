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

        // Define search methods
        private enum SearchMethod
        {
            Exact,
            Prefix,
            Phrase,
            Boolean,
            BooleanNaive
        }

        private static readonly Dictionary<SearchMethod, List<string>> QueryCategories = new()
        {
            [SearchMethod.Exact] = new() { "and", "or", "cat", "because" },
            [SearchMethod.Prefix] = new() { "an*", "or*", "ca*", "br*" },
            [SearchMethod.Phrase] = new() { "it was", "it was a", "and when", "or when" },
            [SearchMethod.Boolean] = new() { "and && or || cat", "cat && because", "because && cat || or", "or && and" },
            [SearchMethod.BooleanNaive] = new() { "and && or || cat", "cat && because", "because && cat || or", "or && and" }
        };

        public static void RunBenchmark()
        {
            var csvPath = "search_benchmark_results.csv";
            using var writer = new StreamWriter(csvPath, false, System.Text.Encoding.UTF8);
            writer.WriteLine("filesize,datastructure,searchmethod,query,time_ms");

            foreach (var fileSize in FileSizes)
            {
                var filePath = Path.Combine(BasePath, $"{fileSize}.txt");
                if (!File.Exists(filePath))
                {
                    Console.WriteLine($"File not found: {filePath}");
                    continue;
                }
                var content = File.ReadAllText(filePath);
                var analyzer = new Analyzer(new SearchEngine.Analysis.Tokenizers.MinimalTokenizer());
                var tokens = analyzer.Analyze(content).ToList();

                Console.WriteLine($"Indexing file size: {fileSize}");
                
                // Prepare batch processing - simulate real document structure
                // Group tokens into "documents" (similar to how real documents would contain multiple tokens)
                const int tokensPerDoc = 100; // Arbitrary document size
                var documentTokens = new List<(int docId, List<Token> tokens)>();
                
                for (int i = 0; i < tokens.Count; i += tokensPerDoc)
                {
                    int docId = i / tokensPerDoc;
                    var docTokens = tokens.Skip(i).Take(tokensPerDoc).ToList();
                    if (docTokens.Count > 0)
                    {
                        documentTokens.Add((docId, docTokens));
                    }
                }
                
                // Index for Trie using batch processing
                var trie = new CompactTrieIndex();
                trie.SetBM25Enabled(false);
                var trieTimer = Stopwatch.StartNew();
                
                // Convert to format expected by AddDocumentsBatch
                var trieDocTokens = documentTokens
                    .Select(dt => (dt.docId, (IEnumerable<Token>)dt.tokens))
                    .ToList();
                trie.AddDocumentsBatch(trieDocTokens);
                
                trieTimer.Stop();
                Console.WriteLine($"Trie indexing time: {trieTimer.ElapsedMilliseconds}ms");

                // Index for InvertedIndex using batch processing
                var inverted = new InvertedIndex();
                inverted.SetBM25Enabled(false);
                var invertedTimer = Stopwatch.StartNew();
                
                // Convert to format expected by AddDocumentsBatch
                var invertedDocTokens = documentTokens
                    .Select(dt => (dt.docId, (IEnumerable<Token>)dt.tokens))
                    .ToList();
                inverted.AddDocumentsBatch(invertedDocTokens);
                
                invertedTimer.Stop();
                Console.WriteLine($"Inverted index indexing time: {invertedTimer.ElapsedMilliseconds}ms");

                foreach (var (searchMethod, queries) in QueryCategories)
                {
                    foreach (var query in queries)
                    {
                        // Trie
                        double totalTimeTrie = 0;
                        for (int i = 0; i < Iterations; i++)
                        {
                            var sw = Stopwatch.StartNew();
                            switch (searchMethod)
                            {
                                case SearchMethod.Exact:
                                    trie.ExactSearch(query);
                                    break;
                                case SearchMethod.Prefix:
                                    trie.PrefixSearch(query.TrimEnd('*'));
                                    break;
                                case SearchMethod.Phrase:
                                    trie.PhraseSearch(query);
                                    break;
                                case SearchMethod.Boolean:
                                    trie.BooleanSearch(query);
                                    break;
                                case SearchMethod.BooleanNaive:
                                    trie.BooleanSearchNaive(query);
                                    break;
                            }
                            sw.Stop();
                            totalTimeTrie += sw.Elapsed.TotalMilliseconds;
                        }
                        double meanTrie = totalTimeTrie / Iterations;
                        writer.WriteLine($"{fileSize},CompactTrieIndex,{searchMethod},{query},{meanTrie.ToString(CultureInfo.InvariantCulture)}");

                        // Inverted Index
                        double totalTimeInv = 0;
                        for (int i = 0; i < Iterations; i++)
                        {
                            var sw = Stopwatch.StartNew();
                            switch (searchMethod)
                            {
                                case SearchMethod.Exact:
                                    inverted.ExactSearch(query);
                                    break;
                                case SearchMethod.Prefix:
                                    inverted.PrefixSearch(query.TrimEnd('*'));
                                    break;
                                case SearchMethod.Phrase:
                                    inverted.PhraseSearch(query);
                                    break;
                                case SearchMethod.Boolean:
                                    inverted.BooleanSearch(query);
                                    break;
                                case SearchMethod.BooleanNaive:
                                    inverted.BooleanSearchNaive(query);
                                    break;
                            }
                            sw.Stop();
                            totalTimeInv += sw.Elapsed.TotalMilliseconds;
                        }
                        double meanInv = totalTimeInv / Iterations;
                        writer.WriteLine($"{fileSize},InvertedIndex,{searchMethod},{query},{meanInv.ToString(CultureInfo.InvariantCulture)}");
                    }
                }
                writer.Flush();
                Console.WriteLine($"Benchmarked file size: {fileSize}");
            }
            Console.WriteLine($"Benchmark complete. Results written to {csvPath}");
        }
    }
}
