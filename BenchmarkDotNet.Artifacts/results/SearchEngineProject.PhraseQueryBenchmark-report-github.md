```

BenchmarkDotNet v0.14.0, Ubuntu 24.04.2 LTS (Noble Numbat)
Intel Core i5-8265U CPU 1.60GHz (Whiskey Lake), 1 CPU, 8 logical and 4 physical cores
.NET SDK 8.0.114
  [Host]     : .NET 8.0.14 (8.0.1425.11118), X64 RyuJIT AVX2
  Job-OPXJDY : .NET 8.0.14 (8.0.1425.11118), X64 RyuJIT AVX2

IterationCount=1  WarmupCount=1  

```
| Method                | FileName  | QueryPhrase    | Mean | Error |
|---------------------- |---------- |--------------- |-----:|------:|
| **BenchmarkPhraseSearch** | **100KB.txt** | **and they**       |   **NA** |    **NA** |
| **BenchmarkPhraseSearch** | **100KB.txt** | **and they were**  |   **NA** |    **NA** |
| **BenchmarkPhraseSearch** | **100KB.txt** | **it was not**     |   **NA** |    **NA** |
| **BenchmarkPhraseSearch** | **100KB.txt** | **they could not** |   **NA** |    **NA** |
| **BenchmarkPhraseSearch** | **1MB.txt**   | **and they**       |   **NA** |    **NA** |
| **BenchmarkPhraseSearch** | **1MB.txt**   | **and they were**  |   **NA** |    **NA** |
| **BenchmarkPhraseSearch** | **1MB.txt**   | **it was not**     |   **NA** |    **NA** |
| **BenchmarkPhraseSearch** | **1MB.txt**   | **they could not** |   **NA** |    **NA** |

Benchmarks with issues:
  PhraseQueryBenchmark.BenchmarkPhraseSearch: Job-OPXJDY(IterationCount=1, WarmupCount=1) [FileName=100KB.txt, QueryPhrase=and they]
  PhraseQueryBenchmark.BenchmarkPhraseSearch: Job-OPXJDY(IterationCount=1, WarmupCount=1) [FileName=100KB.txt, QueryPhrase=and they were]
  PhraseQueryBenchmark.BenchmarkPhraseSearch: Job-OPXJDY(IterationCount=1, WarmupCount=1) [FileName=100KB.txt, QueryPhrase=it was not]
  PhraseQueryBenchmark.BenchmarkPhraseSearch: Job-OPXJDY(IterationCount=1, WarmupCount=1) [FileName=100KB.txt, QueryPhrase=they could not]
  PhraseQueryBenchmark.BenchmarkPhraseSearch: Job-OPXJDY(IterationCount=1, WarmupCount=1) [FileName=1MB.txt, QueryPhrase=and they]
  PhraseQueryBenchmark.BenchmarkPhraseSearch: Job-OPXJDY(IterationCount=1, WarmupCount=1) [FileName=1MB.txt, QueryPhrase=and they were]
  PhraseQueryBenchmark.BenchmarkPhraseSearch: Job-OPXJDY(IterationCount=1, WarmupCount=1) [FileName=1MB.txt, QueryPhrase=it was not]
  PhraseQueryBenchmark.BenchmarkPhraseSearch: Job-OPXJDY(IterationCount=1, WarmupCount=1) [FileName=1MB.txt, QueryPhrase=they could not]
