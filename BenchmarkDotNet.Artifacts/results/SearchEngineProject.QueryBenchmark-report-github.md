```

BenchmarkDotNet v0.14.0, Ubuntu 24.04.2 LTS (Noble Numbat)
Intel Core i5-8265U CPU 1.60GHz (Whiskey Lake), 1 CPU, 8 logical and 4 physical cores
.NET SDK 8.0.114
  [Host]     : .NET 8.0.14 (8.0.1425.11118), X64 RyuJIT AVX2
  DefaultJob : .NET 8.0.14 (8.0.1425.11118), X64 RyuJIT AVX2


```
| Method          | FileName  | Query | Mean | Error |
|---------------- |---------- |------ |-----:|------:|
| **BenchmarkSearch** | **100KB.txt** | **and**   |   **NA** |    **NA** |
| **BenchmarkSearch** | **100KB.txt** | **bread** |   **NA** |    **NA** |
| **BenchmarkSearch** | **100KB.txt** | **cat**   |   **NA** |    **NA** |
| **BenchmarkSearch** | **100KB.txt** | **or**    |   **NA** |    **NA** |
| **BenchmarkSearch** | **100MB.txt** | **and**   |   **NA** |    **NA** |
| **BenchmarkSearch** | **100MB.txt** | **bread** |   **NA** |    **NA** |
| **BenchmarkSearch** | **100MB.txt** | **cat**   |   **NA** |    **NA** |
| **BenchmarkSearch** | **100MB.txt** | **or**    |   **NA** |    **NA** |
| **BenchmarkSearch** | **10MB.txt**  | **and**   |   **NA** |    **NA** |
| **BenchmarkSearch** | **10MB.txt**  | **bread** |   **NA** |    **NA** |
| **BenchmarkSearch** | **10MB.txt**  | **cat**   |   **NA** |    **NA** |
| **BenchmarkSearch** | **10MB.txt**  | **or**    |   **NA** |    **NA** |
| **BenchmarkSearch** | **1MB.txt**   | **and**   |   **NA** |    **NA** |
| **BenchmarkSearch** | **1MB.txt**   | **bread** |   **NA** |    **NA** |
| **BenchmarkSearch** | **1MB.txt**   | **cat**   |   **NA** |    **NA** |
| **BenchmarkSearch** | **1MB.txt**   | **or**    |   **NA** |    **NA** |
| **BenchmarkSearch** | **20MB.txt**  | **and**   |   **NA** |    **NA** |
| **BenchmarkSearch** | **20MB.txt**  | **bread** |   **NA** |    **NA** |
| **BenchmarkSearch** | **20MB.txt**  | **cat**   |   **NA** |    **NA** |
| **BenchmarkSearch** | **20MB.txt**  | **or**    |   **NA** |    **NA** |
| **BenchmarkSearch** | **2MB.txt**   | **and**   |   **NA** |    **NA** |
| **BenchmarkSearch** | **2MB.txt**   | **bread** |   **NA** |    **NA** |
| **BenchmarkSearch** | **2MB.txt**   | **cat**   |   **NA** |    **NA** |
| **BenchmarkSearch** | **2MB.txt**   | **or**    |   **NA** |    **NA** |
| **BenchmarkSearch** | **50MB.txt**  | **and**   |   **NA** |    **NA** |
| **BenchmarkSearch** | **50MB.txt**  | **bread** |   **NA** |    **NA** |
| **BenchmarkSearch** | **50MB.txt**  | **cat**   |   **NA** |    **NA** |
| **BenchmarkSearch** | **50MB.txt**  | **or**    |   **NA** |    **NA** |
| **BenchmarkSearch** | **5MB.txt**   | **and**   |   **NA** |    **NA** |
| **BenchmarkSearch** | **5MB.txt**   | **bread** |   **NA** |    **NA** |
| **BenchmarkSearch** | **5MB.txt**   | **cat**   |   **NA** |    **NA** |
| **BenchmarkSearch** | **5MB.txt**   | **or**    |   **NA** |    **NA** |

Benchmarks with issues:
  QueryBenchmark.BenchmarkSearch: DefaultJob [FileName=100KB.txt, Query=and]
  QueryBenchmark.BenchmarkSearch: DefaultJob [FileName=100KB.txt, Query=bread]
  QueryBenchmark.BenchmarkSearch: DefaultJob [FileName=100KB.txt, Query=cat]
  QueryBenchmark.BenchmarkSearch: DefaultJob [FileName=100KB.txt, Query=or]
  QueryBenchmark.BenchmarkSearch: DefaultJob [FileName=100MB.txt, Query=and]
  QueryBenchmark.BenchmarkSearch: DefaultJob [FileName=100MB.txt, Query=bread]
  QueryBenchmark.BenchmarkSearch: DefaultJob [FileName=100MB.txt, Query=cat]
  QueryBenchmark.BenchmarkSearch: DefaultJob [FileName=100MB.txt, Query=or]
  QueryBenchmark.BenchmarkSearch: DefaultJob [FileName=10MB.txt, Query=and]
  QueryBenchmark.BenchmarkSearch: DefaultJob [FileName=10MB.txt, Query=bread]
  QueryBenchmark.BenchmarkSearch: DefaultJob [FileName=10MB.txt, Query=cat]
  QueryBenchmark.BenchmarkSearch: DefaultJob [FileName=10MB.txt, Query=or]
  QueryBenchmark.BenchmarkSearch: DefaultJob [FileName=1MB.txt, Query=and]
  QueryBenchmark.BenchmarkSearch: DefaultJob [FileName=1MB.txt, Query=bread]
  QueryBenchmark.BenchmarkSearch: DefaultJob [FileName=1MB.txt, Query=cat]
  QueryBenchmark.BenchmarkSearch: DefaultJob [FileName=1MB.txt, Query=or]
  QueryBenchmark.BenchmarkSearch: DefaultJob [FileName=20MB.txt, Query=and]
  QueryBenchmark.BenchmarkSearch: DefaultJob [FileName=20MB.txt, Query=bread]
  QueryBenchmark.BenchmarkSearch: DefaultJob [FileName=20MB.txt, Query=cat]
  QueryBenchmark.BenchmarkSearch: DefaultJob [FileName=20MB.txt, Query=or]
  QueryBenchmark.BenchmarkSearch: DefaultJob [FileName=2MB.txt, Query=and]
  QueryBenchmark.BenchmarkSearch: DefaultJob [FileName=2MB.txt, Query=bread]
  QueryBenchmark.BenchmarkSearch: DefaultJob [FileName=2MB.txt, Query=cat]
  QueryBenchmark.BenchmarkSearch: DefaultJob [FileName=2MB.txt, Query=or]
  QueryBenchmark.BenchmarkSearch: DefaultJob [FileName=50MB.txt, Query=and]
  QueryBenchmark.BenchmarkSearch: DefaultJob [FileName=50MB.txt, Query=bread]
  QueryBenchmark.BenchmarkSearch: DefaultJob [FileName=50MB.txt, Query=cat]
  QueryBenchmark.BenchmarkSearch: DefaultJob [FileName=50MB.txt, Query=or]
  QueryBenchmark.BenchmarkSearch: DefaultJob [FileName=5MB.txt, Query=and]
  QueryBenchmark.BenchmarkSearch: DefaultJob [FileName=5MB.txt, Query=bread]
  QueryBenchmark.BenchmarkSearch: DefaultJob [FileName=5MB.txt, Query=cat]
  QueryBenchmark.BenchmarkSearch: DefaultJob [FileName=5MB.txt, Query=or]
