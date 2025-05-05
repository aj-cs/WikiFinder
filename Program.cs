using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Sqlite;
using Microsoft.EntityFrameworkCore.SqlServer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SearchEngine.Analysis;
using SearchEngine.Analysis.Interfaces;
using SearchEngine.Core;
using SearchEngine.Core.Interfaces;
using SearchEngine.Persistence;
using SearchEngine.Services;
using SearchEngine.Services.Interfaces;
using SearchEngine.Analysis.Tokenizers;
using SearchEngine.Analysis.Filters;
using Porter2StemmerStandard;

var host = Host.CreateDefaultBuilder(args)
               .ConfigureAppConfiguration((ctx, cfg) =>
               {
                   cfg.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true) // check optional from storyline later
                      .AddEnvironmentVariables();
               })
               .ConfigureServices((ctx, services) =>
               {
                   //database and persistence
                   services.AddDbContext<SearchEngineContext>(opts => opts.UseSqlite("Data Source=quicktest.db"));
                   // opts.UseSqlServer(ctx.Configuration.GetConnectionString("SearchEngine")));
                   services.AddScoped<DocumentRepository>();
                   services.AddScoped<DocumentTermRepository>();
                   services.AddScoped<IDocumentService, DocumentService>();
                   var stopWords = new List<string>{"a", "the", "and", "or", "of", "on", "this", "we", "were",
                           "is", "not"};
                   services.AddScoped<ITokenFilter, StopWordFilter>();

                   //analysis pipeline
                   services.AddSingleton<Analyzer>(sp =>
                       new Analyzer(
                           new MinimalTokenizer(),
                       // add filters here later
                       new PorterStemFilter(new EnglishPorter2Stemmer())

                       )
                   );
                   // indexingdd and search
                   services.AddSingleton<IExactPrefixIndex, CompactTrieIndex>();
                   services.AddSingleton<IFullTextIndex, InvertedIndex>();
                   services.AddSingleton<IBloomFilter>(sp => new BloomFilter(1000000, 0.01)); // 1M expected items, 1% false positive rate
                   services.AddScoped<IIndexingService, IndexingService>();

                   //extensible search operations go here, facade pattern i think
                   services.AddSingleton<ISearchOperation, ExactSearchOperation>();
                   services.AddSingleton<ISearchOperation, PrefixDocsSearchOperation>();
                   services.AddSingleton<ISearchOperation, AutoCompleteSearchOperation>();
                   services.AddSingleton<ISearchOperation, FullTextSearchOperation>();
                   services.AddSingleton<ISearchOperation, BloomFilterSearchOperation>();
                   services.AddScoped<ISearchService, SearchService>();

               })
               .Build();
using var scope = host.Services.CreateScope();
var docService = scope.ServiceProvider.GetRequiredService<IDocumentService>();
var indexer = scope.ServiceProvider.GetRequiredService<IIndexingService>();
var searchService = scope.ServiceProvider.GetRequiredService<ISearchService>();

// ensure database is created
var dbContext = scope.ServiceProvider.GetRequiredService<SearchEngineContext>();
dbContext.Database.EnsureCreated();

if (args.Length < 1)
{
    Console.WriteLine("Usage: dotnet run <filename>");
    return;
}

var filename = args[0];
Console.WriteLine($"Preprocessing {filename}…");
var stopwatch = Stopwatch.StartNew();

using var reader = new StreamReader(filename, Encoding.UTF8);
string? line;
string? currentTitle = null;
bool titleRead = false;
var sb = new StringBuilder();

while ((line = reader.ReadLine()) != null)
{
    if (string.IsNullOrWhiteSpace(line))
    {
        continue;
    }

    if (line == "---END.OF.DOCUMENT---")
    {
        if (titleRead && currentTitle is not null)
        {
            await indexer.AddDocumentAsync(currentTitle, sb.ToString());
        }

        titleRead = false;
        currentTitle = null;
        sb.Clear();
        continue;
    }

    if (!titleRead)
    {
        currentTitle = line;
        titleRead = true;
    }
    else
    {
        sb.AppendLine(line);
    }
}

// flush last document if file didn't end with the marker
if (titleRead && currentTitle is not null)
{
    await indexer.AddDocumentAsync(currentTitle, sb.ToString());
}

stopwatch.Stop();
Console.WriteLine($"Pre-process time: {stopwatch.Elapsed.TotalSeconds:F3}s");
Console.WriteLine("Ready for queries.  Type `<operation> <query>` or `exit`.");

while (true)
{
    Console.Write("> ");
    var input = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(input) ||
        input.Equals("exit", StringComparison.OrdinalIgnoreCase))
    {
        break;
    }

    var parts = input.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
    var op = parts[0];
    var query = parts.Length > 1 ? parts[1] : "";

    try
    {
        var raw = await searchService.SearchAsync(op, query);

        switch (raw)
        {
            case bool b:
                Console.WriteLine(b);
                break;

            case List<string> list:
                Console.WriteLine(string.Join(", ", list));
                break;

            case List<(string word, List<string> titles)> ac:
                foreach (var (word, titles) in ac)
                {
                    Console.WriteLine($"{word}: {string.Join(", ", titles)}");
                }
                break;

            default:
                Console.WriteLine(raw);
                break;
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
    }
}

await host.StopAsync();

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

// public class IndexConfig : ManualConfig
// {
//     public IndexConfig()
//     {
//         AddJob(Job.Default
//                 .WithWarmupCount(1)
//                 .WithIterationCount(1)
//                 .WithInvocationCount(1)
//                 .WithUnrollFactor(1)
//                 .WithId("IndexConstructionJob"));
//     }
// }
// public class QueryConfig : ManualConfig
// {
//     public QueryConfig()
//     {
//         AddJob(Job.Default
//                 .WithWarmupCount(1)
//                 .WithIterationCount(3)
//                 .WithInvocationCount(1)
//                 .WithUnrollFactor(1)
//                 .WithId("QueryJob"));
//     }
// }
// [CsvExporter]
// [MemoryDiagnoser]
// public class IndexConstructionBenchmark
// {
//     // Parameterized list of file names.
//     // (These file names are relative to your project directory.)
//     [Params("100KB.txt", "1MB.txt", "2MB.txt", "5MB.txt", "10MB.txt", "20MB.txt", "50MB.txt", "100MB.txt")]
//     public string FileName { get; set; }
//
//     [Benchmark]
//     public Index BenchmarkIndexConstruction()
//     {
//         // Use an absolute path to your project directory
//         string projectDir = "/zhome/79/1/188120/search-engine-project";
//         string fullPath = System.IO.Path.Combine(projectDir, FileName);
//         return new Index(fullPath);
//     }
// }
//
// // This benchmark class measures the time and memory of search queries individually.
// [CsvExporter]
// [MemoryDiagnoser]
// public class QueryBenchmark
// {
//     // Parameterized file name for building the index.
//     [Params("100KB.txt", "1MB.txt", "2MB.txt", "5MB.txt", "10MB.txt", "20MB.txt", "50MB.txt", "100MB.txt")]
//     public string FileName { get; set; }
//
//     // Parameterized query so that each query ("and", "or", "cat", "bread") is benchmarked separately.
//     [Params("and", "or", "cat", "bread")]
//     public string Query { get; set; }
//
//     private Index index;
//
//     [GlobalSetup]
//     public void Setup()
//     {
//         string projectDir = "/zhome/79/1/188120/search-engine-project";
//         string fullPath = System.IO.Path.Combine(projectDir, FileName);
//         index = new Index(fullPath);
//     }
//
//     // Measures the time for the PrefixSearch method for a single query.
//     [Benchmark]
//     public void BenchmarkPrefixSearch()
//     {
//         index.PrefixSearch(Query);
//     }
//
//     // Measures the time for the PrefixSearchDocuments method for a single query.
//     [Benchmark]
//     public void BenchmarkPrefixSearchDocuments()
//     {
//         index.PrefixSearchDocuments(Query);
//     }
//
//     //Measures the time for the Normal Search
//     [Benchmark]
//     public void BenchmarkNormalSearchDocuments()
//     {
//         index.PrefixSearchDocuments(Query);
//     }
// }
//
// // The Program class runs both sets of benchmarks.
// public class Program
// {
//     public static void Main(string[] args)
//     {
//         // Run the index construction benchmarks.
//         // BenchmarkRunner.Run<IndexConstructionBenchmark>();
//
//         // Run the query benchmarks
//         // BenchmarkRunner.Run<QueryBenchmark>();
//         if (args.Length == 0)
//         {
//             Console.WriteLine("Usage: Index1 <filename>");
//             return;
//         }
//         var stopwatch = new Stopwatch();
//         Console.WriteLine("Preprocessing " + args[0]);
//         stopwatch.Start();
//         Index index = new Index(args[0]);
//         stopwatch.Stop();
//         Console.WriteLine($"Pre-process time(s) {stopwatch.ElapsedMilliseconds / 1000}");
//
//         while (true)
//         {
//             Console.WriteLine("Input search string or type exit to stop");
//             string searchStr = Console.ReadLine();
//
//             if (searchStr.Equals("exit", StringComparison.OrdinalIgnoreCase))
//             {
//                 break;
//             }
//
//             index.PrintDemo(searchStr, searchStr);
//         }
//     }
// }
// /*class Program
// {
//     static void Main(string[] args)
//     {
//         if (args.Length == 0)
//         {
//             Console.WriteLine("Usage: Index1 <filename>");
//             return;
//         }
//
//         Console.WriteLine("Preprocessing " + args[0]);
//         Index index = new Index(args[0]);
//
//         while (true)
//         {
//             Console.WriteLine("Input search string or type exit to stop");
//             string searchStr = Console.ReadLine();
//
//             if (searchStr.Equals("exit", StringComparison.OrdinalIgnoreCase))
//             {
//                 break;
//             }
//
//             index.PrefixSearchDocuments(searchStr);
//             Console.WriteLine($"\nAuto-completion of words starting with '{searchStr}': ");
//             index.PrefixSearch(searchStr);
//         }
//     }
//     
// }*/
//
