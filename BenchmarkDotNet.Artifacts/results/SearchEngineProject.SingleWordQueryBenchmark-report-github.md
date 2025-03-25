```

BenchmarkDotNet v0.14.0, Ubuntu 24.04.2 LTS (Noble Numbat)
Intel Core i5-8265U CPU 1.60GHz (Whiskey Lake), 1 CPU, 8 logical and 4 physical cores
.NET SDK 8.0.114
  [Host]     : .NET 8.0.14 (8.0.1425.11118), X64 RyuJIT AVX2
  Job-OPXJDY : .NET 8.0.14 (8.0.1425.11118), X64 RyuJIT AVX2

IterationCount=1  WarmupCount=1  

```
| Method                         | FileName  | Query | Mean | Error |
|------------------------------- |---------- |------ |-----:|------:|
| **BenchmarkPrefixSearchIndex**     | **100KB.txt** | **and**   |   **NA** |    **NA** |
| BenchmarkNormalSearchDocuments | 100KB.txt | and   |   NA |    NA |
| BenchmarkRankingSearch         | 100KB.txt | and   |   NA |    NA |
| **BenchmarkPrefixSearchIndex**     | **100KB.txt** | **bread** |   **NA** |    **NA** |
| BenchmarkNormalSearchDocuments | 100KB.txt | bread |   NA |    NA |
| BenchmarkRankingSearch         | 100KB.txt | bread |   NA |    NA |
| **BenchmarkPrefixSearchIndex**     | **100KB.txt** | **cat**   |   **NA** |    **NA** |
| BenchmarkNormalSearchDocuments | 100KB.txt | cat   |   NA |    NA |
| BenchmarkRankingSearch         | 100KB.txt | cat   |   NA |    NA |
| **BenchmarkPrefixSearchIndex**     | **100KB.txt** | **or**    |   **NA** |    **NA** |
| BenchmarkNormalSearchDocuments | 100KB.txt | or    |   NA |    NA |
| BenchmarkRankingSearch         | 100KB.txt | or    |   NA |    NA |
| **BenchmarkPrefixSearchIndex**     | **1MB.txt**   | **and**   |   **NA** |    **NA** |
| BenchmarkNormalSearchDocuments | 1MB.txt   | and   |   NA |    NA |
| BenchmarkRankingSearch         | 1MB.txt   | and   |   NA |    NA |
| **BenchmarkPrefixSearchIndex**     | **1MB.txt**   | **bread** |   **NA** |    **NA** |
| BenchmarkNormalSearchDocuments | 1MB.txt   | bread |   NA |    NA |
| BenchmarkRankingSearch         | 1MB.txt   | bread |   NA |    NA |
| **BenchmarkPrefixSearchIndex**     | **1MB.txt**   | **cat**   |   **NA** |    **NA** |
| BenchmarkNormalSearchDocuments | 1MB.txt   | cat   |   NA |    NA |
| BenchmarkRankingSearch         | 1MB.txt   | cat   |   NA |    NA |
| **BenchmarkPrefixSearchIndex**     | **1MB.txt**   | **or**    |   **NA** |    **NA** |
| BenchmarkNormalSearchDocuments | 1MB.txt   | or    |   NA |    NA |
| BenchmarkRankingSearch         | 1MB.txt   | or    |   NA |    NA |

Benchmarks with issues:
  SingleWordQueryBenchmark.BenchmarkPrefixSearchIndex: Job-OPXJDY(IterationCount=1, WarmupCount=1) [FileName=100KB.txt, Query=and]
  SingleWordQueryBenchmark.BenchmarkNormalSearchDocuments: Job-OPXJDY(IterationCount=1, WarmupCount=1) [FileName=100KB.txt, Query=and]
  SingleWordQueryBenchmark.BenchmarkRankingSearch: Job-OPXJDY(IterationCount=1, WarmupCount=1) [FileName=100KB.txt, Query=and]
  SingleWordQueryBenchmark.BenchmarkPrefixSearchIndex: Job-OPXJDY(IterationCount=1, WarmupCount=1) [FileName=100KB.txt, Query=bread]
  SingleWordQueryBenchmark.BenchmarkNormalSearchDocuments: Job-OPXJDY(IterationCount=1, WarmupCount=1) [FileName=100KB.txt, Query=bread]
  SingleWordQueryBenchmark.BenchmarkRankingSearch: Job-OPXJDY(IterationCount=1, WarmupCount=1) [FileName=100KB.txt, Query=bread]
  SingleWordQueryBenchmark.BenchmarkPrefixSearchIndex: Job-OPXJDY(IterationCount=1, WarmupCount=1) [FileName=100KB.txt, Query=cat]
  SingleWordQueryBenchmark.BenchmarkNormalSearchDocuments: Job-OPXJDY(IterationCount=1, WarmupCount=1) [FileName=100KB.txt, Query=cat]
  SingleWordQueryBenchmark.BenchmarkRankingSearch: Job-OPXJDY(IterationCount=1, WarmupCount=1) [FileName=100KB.txt, Query=cat]
  SingleWordQueryBenchmark.BenchmarkPrefixSearchIndex: Job-OPXJDY(IterationCount=1, WarmupCount=1) [FileName=100KB.txt, Query=or]
  SingleWordQueryBenchmark.BenchmarkNormalSearchDocuments: Job-OPXJDY(IterationCount=1, WarmupCount=1) [FileName=100KB.txt, Query=or]
  SingleWordQueryBenchmark.BenchmarkRankingSearch: Job-OPXJDY(IterationCount=1, WarmupCount=1) [FileName=100KB.txt, Query=or]
  SingleWordQueryBenchmark.BenchmarkPrefixSearchIndex: Job-OPXJDY(IterationCount=1, WarmupCount=1) [FileName=1MB.txt, Query=and]
  SingleWordQueryBenchmark.BenchmarkNormalSearchDocuments: Job-OPXJDY(IterationCount=1, WarmupCount=1) [FileName=1MB.txt, Query=and]
  SingleWordQueryBenchmark.BenchmarkRankingSearch: Job-OPXJDY(IterationCount=1, WarmupCount=1) [FileName=1MB.txt, Query=and]
  SingleWordQueryBenchmark.BenchmarkPrefixSearchIndex: Job-OPXJDY(IterationCount=1, WarmupCount=1) [FileName=1MB.txt, Query=bread]
  SingleWordQueryBenchmark.BenchmarkNormalSearchDocuments: Job-OPXJDY(IterationCount=1, WarmupCount=1) [FileName=1MB.txt, Query=bread]
  SingleWordQueryBenchmark.BenchmarkRankingSearch: Job-OPXJDY(IterationCount=1, WarmupCount=1) [FileName=1MB.txt, Query=bread]
  SingleWordQueryBenchmark.BenchmarkPrefixSearchIndex: Job-OPXJDY(IterationCount=1, WarmupCount=1) [FileName=1MB.txt, Query=cat]
  SingleWordQueryBenchmark.BenchmarkNormalSearchDocuments: Job-OPXJDY(IterationCount=1, WarmupCount=1) [FileName=1MB.txt, Query=cat]
  SingleWordQueryBenchmark.BenchmarkRankingSearch: Job-OPXJDY(IterationCount=1, WarmupCount=1) [FileName=1MB.txt, Query=cat]
  SingleWordQueryBenchmark.BenchmarkPrefixSearchIndex: Job-OPXJDY(IterationCount=1, WarmupCount=1) [FileName=1MB.txt, Query=or]
  SingleWordQueryBenchmark.BenchmarkNormalSearchDocuments: Job-OPXJDY(IterationCount=1, WarmupCount=1) [FileName=1MB.txt, Query=or]
  SingleWordQueryBenchmark.BenchmarkRankingSearch: Job-OPXJDY(IterationCount=1, WarmupCount=1) [FileName=1MB.txt, Query=or]
