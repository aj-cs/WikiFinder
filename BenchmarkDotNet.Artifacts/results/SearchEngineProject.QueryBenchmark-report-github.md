```

BenchmarkDotNet v0.14.0, Ubuntu 24.04.2 LTS (Noble Numbat)
11th Gen Intel Core i7-11370H 3.30GHz, 1 CPU, 8 logical and 4 physical cores
.NET SDK 8.0.115
  [Host]     : .NET 8.0.15 (8.0.1525.16413), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  DefaultJob : .NET 8.0.15 (8.0.1525.16413), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI


```
| Method                         | FileName  | Query | Mean | Error |
|------------------------------- |---------- |------ |-----:|------:|
| **BenchmarkPrefixSearch**          | **100KB.txt** | **and**   |   **NA** |    **NA** |
| BenchmarkPrefixSearchDocuments | 100KB.txt | and   |   NA |    NA |
| BenchmarkNormalSearchDocuments | 100KB.txt | and   |   NA |    NA |
| **BenchmarkPrefixSearch**          | **100KB.txt** | **bread** |   **NA** |    **NA** |
| BenchmarkPrefixSearchDocuments | 100KB.txt | bread |   NA |    NA |
| BenchmarkNormalSearchDocuments | 100KB.txt | bread |   NA |    NA |
| **BenchmarkPrefixSearch**          | **100KB.txt** | **cat**   |   **NA** |    **NA** |
| BenchmarkPrefixSearchDocuments | 100KB.txt | cat   |   NA |    NA |
| BenchmarkNormalSearchDocuments | 100KB.txt | cat   |   NA |    NA |
| **BenchmarkPrefixSearch**          | **100KB.txt** | **or**    |   **NA** |    **NA** |
| BenchmarkPrefixSearchDocuments | 100KB.txt | or    |   NA |    NA |
| BenchmarkNormalSearchDocuments | 100KB.txt | or    |   NA |    NA |
| **BenchmarkPrefixSearch**          | **100MB.txt** | **and**   |   **NA** |    **NA** |
| BenchmarkPrefixSearchDocuments | 100MB.txt | and   |   NA |    NA |
| BenchmarkNormalSearchDocuments | 100MB.txt | and   |   NA |    NA |
| **BenchmarkPrefixSearch**          | **100MB.txt** | **bread** |   **NA** |    **NA** |
| BenchmarkPrefixSearchDocuments | 100MB.txt | bread |   NA |    NA |
| BenchmarkNormalSearchDocuments | 100MB.txt | bread |   NA |    NA |
| **BenchmarkPrefixSearch**          | **100MB.txt** | **cat**   |   **NA** |    **NA** |
| BenchmarkPrefixSearchDocuments | 100MB.txt | cat   |   NA |    NA |
| BenchmarkNormalSearchDocuments | 100MB.txt | cat   |   NA |    NA |
| **BenchmarkPrefixSearch**          | **100MB.txt** | **or**    |   **NA** |    **NA** |
| BenchmarkPrefixSearchDocuments | 100MB.txt | or    |   NA |    NA |
| BenchmarkNormalSearchDocuments | 100MB.txt | or    |   NA |    NA |
| **BenchmarkPrefixSearch**          | **10MB.txt**  | **and**   |   **NA** |    **NA** |
| BenchmarkPrefixSearchDocuments | 10MB.txt  | and   |   NA |    NA |
| BenchmarkNormalSearchDocuments | 10MB.txt  | and   |   NA |    NA |
| **BenchmarkPrefixSearch**          | **10MB.txt**  | **bread** |   **NA** |    **NA** |
| BenchmarkPrefixSearchDocuments | 10MB.txt  | bread |   NA |    NA |
| BenchmarkNormalSearchDocuments | 10MB.txt  | bread |   NA |    NA |
| **BenchmarkPrefixSearch**          | **10MB.txt**  | **cat**   |   **NA** |    **NA** |
| BenchmarkPrefixSearchDocuments | 10MB.txt  | cat   |   NA |    NA |
| BenchmarkNormalSearchDocuments | 10MB.txt  | cat   |   NA |    NA |
| **BenchmarkPrefixSearch**          | **10MB.txt**  | **or**    |   **NA** |    **NA** |
| BenchmarkPrefixSearchDocuments | 10MB.txt  | or    |   NA |    NA |
| BenchmarkNormalSearchDocuments | 10MB.txt  | or    |   NA |    NA |
| **BenchmarkPrefixSearch**          | **1MB.txt**   | **and**   |   **NA** |    **NA** |
| BenchmarkPrefixSearchDocuments | 1MB.txt   | and   |   NA |    NA |
| BenchmarkNormalSearchDocuments | 1MB.txt   | and   |   NA |    NA |
| **BenchmarkPrefixSearch**          | **1MB.txt**   | **bread** |   **NA** |    **NA** |
| BenchmarkPrefixSearchDocuments | 1MB.txt   | bread |   NA |    NA |
| BenchmarkNormalSearchDocuments | 1MB.txt   | bread |   NA |    NA |
| **BenchmarkPrefixSearch**          | **1MB.txt**   | **cat**   |   **NA** |    **NA** |
| BenchmarkPrefixSearchDocuments | 1MB.txt   | cat   |   NA |    NA |
| BenchmarkNormalSearchDocuments | 1MB.txt   | cat   |   NA |    NA |
| **BenchmarkPrefixSearch**          | **1MB.txt**   | **or**    |   **NA** |    **NA** |
| BenchmarkPrefixSearchDocuments | 1MB.txt   | or    |   NA |    NA |
| BenchmarkNormalSearchDocuments | 1MB.txt   | or    |   NA |    NA |
| **BenchmarkPrefixSearch**          | **20MB.txt**  | **and**   |   **NA** |    **NA** |
| BenchmarkPrefixSearchDocuments | 20MB.txt  | and   |   NA |    NA |
| BenchmarkNormalSearchDocuments | 20MB.txt  | and   |   NA |    NA |
| **BenchmarkPrefixSearch**          | **20MB.txt**  | **bread** |   **NA** |    **NA** |
| BenchmarkPrefixSearchDocuments | 20MB.txt  | bread |   NA |    NA |
| BenchmarkNormalSearchDocuments | 20MB.txt  | bread |   NA |    NA |
| **BenchmarkPrefixSearch**          | **20MB.txt**  | **cat**   |   **NA** |    **NA** |
| BenchmarkPrefixSearchDocuments | 20MB.txt  | cat   |   NA |    NA |
| BenchmarkNormalSearchDocuments | 20MB.txt  | cat   |   NA |    NA |
| **BenchmarkPrefixSearch**          | **20MB.txt**  | **or**    |   **NA** |    **NA** |
| BenchmarkPrefixSearchDocuments | 20MB.txt  | or    |   NA |    NA |
| BenchmarkNormalSearchDocuments | 20MB.txt  | or    |   NA |    NA |
| **BenchmarkPrefixSearch**          | **2MB.txt**   | **and**   |   **NA** |    **NA** |
| BenchmarkPrefixSearchDocuments | 2MB.txt   | and   |   NA |    NA |
| BenchmarkNormalSearchDocuments | 2MB.txt   | and   |   NA |    NA |
| **BenchmarkPrefixSearch**          | **2MB.txt**   | **bread** |   **NA** |    **NA** |
| BenchmarkPrefixSearchDocuments | 2MB.txt   | bread |   NA |    NA |
| BenchmarkNormalSearchDocuments | 2MB.txt   | bread |   NA |    NA |
| **BenchmarkPrefixSearch**          | **2MB.txt**   | **cat**   |   **NA** |    **NA** |
| BenchmarkPrefixSearchDocuments | 2MB.txt   | cat   |   NA |    NA |
| BenchmarkNormalSearchDocuments | 2MB.txt   | cat   |   NA |    NA |
| **BenchmarkPrefixSearch**          | **2MB.txt**   | **or**    |   **NA** |    **NA** |
| BenchmarkPrefixSearchDocuments | 2MB.txt   | or    |   NA |    NA |
| BenchmarkNormalSearchDocuments | 2MB.txt   | or    |   NA |    NA |
| **BenchmarkPrefixSearch**          | **50MB.txt**  | **and**   |   **NA** |    **NA** |
| BenchmarkPrefixSearchDocuments | 50MB.txt  | and   |   NA |    NA |
| BenchmarkNormalSearchDocuments | 50MB.txt  | and   |   NA |    NA |
| **BenchmarkPrefixSearch**          | **50MB.txt**  | **bread** |   **NA** |    **NA** |
| BenchmarkPrefixSearchDocuments | 50MB.txt  | bread |   NA |    NA |
| BenchmarkNormalSearchDocuments | 50MB.txt  | bread |   NA |    NA |
| **BenchmarkPrefixSearch**          | **50MB.txt**  | **cat**   |   **NA** |    **NA** |
| BenchmarkPrefixSearchDocuments | 50MB.txt  | cat   |   NA |    NA |
| BenchmarkNormalSearchDocuments | 50MB.txt  | cat   |   NA |    NA |
| **BenchmarkPrefixSearch**          | **50MB.txt**  | **or**    |   **NA** |    **NA** |
| BenchmarkPrefixSearchDocuments | 50MB.txt  | or    |   NA |    NA |
| BenchmarkNormalSearchDocuments | 50MB.txt  | or    |   NA |    NA |
| **BenchmarkPrefixSearch**          | **5MB.txt**   | **and**   |   **NA** |    **NA** |
| BenchmarkPrefixSearchDocuments | 5MB.txt   | and   |   NA |    NA |
| BenchmarkNormalSearchDocuments | 5MB.txt   | and   |   NA |    NA |
| **BenchmarkPrefixSearch**          | **5MB.txt**   | **bread** |   **NA** |    **NA** |
| BenchmarkPrefixSearchDocuments | 5MB.txt   | bread |   NA |    NA |
| BenchmarkNormalSearchDocuments | 5MB.txt   | bread |   NA |    NA |
| **BenchmarkPrefixSearch**          | **5MB.txt**   | **cat**   |   **NA** |    **NA** |
| BenchmarkPrefixSearchDocuments | 5MB.txt   | cat   |   NA |    NA |
| BenchmarkNormalSearchDocuments | 5MB.txt   | cat   |   NA |    NA |
| **BenchmarkPrefixSearch**          | **5MB.txt**   | **or**    |   **NA** |    **NA** |
| BenchmarkPrefixSearchDocuments | 5MB.txt   | or    |   NA |    NA |
| BenchmarkNormalSearchDocuments | 5MB.txt   | or    |   NA |    NA |

Benchmarks with issues:
  QueryBenchmark.BenchmarkPrefixSearch: DefaultJob [FileName=100KB.txt, Query=and]
  QueryBenchmark.BenchmarkPrefixSearchDocuments: DefaultJob [FileName=100KB.txt, Query=and]
  QueryBenchmark.BenchmarkNormalSearchDocuments: DefaultJob [FileName=100KB.txt, Query=and]
  QueryBenchmark.BenchmarkPrefixSearch: DefaultJob [FileName=100KB.txt, Query=bread]
  QueryBenchmark.BenchmarkPrefixSearchDocuments: DefaultJob [FileName=100KB.txt, Query=bread]
  QueryBenchmark.BenchmarkNormalSearchDocuments: DefaultJob [FileName=100KB.txt, Query=bread]
  QueryBenchmark.BenchmarkPrefixSearch: DefaultJob [FileName=100KB.txt, Query=cat]
  QueryBenchmark.BenchmarkPrefixSearchDocuments: DefaultJob [FileName=100KB.txt, Query=cat]
  QueryBenchmark.BenchmarkNormalSearchDocuments: DefaultJob [FileName=100KB.txt, Query=cat]
  QueryBenchmark.BenchmarkPrefixSearch: DefaultJob [FileName=100KB.txt, Query=or]
  QueryBenchmark.BenchmarkPrefixSearchDocuments: DefaultJob [FileName=100KB.txt, Query=or]
  QueryBenchmark.BenchmarkNormalSearchDocuments: DefaultJob [FileName=100KB.txt, Query=or]
  QueryBenchmark.BenchmarkPrefixSearch: DefaultJob [FileName=100MB.txt, Query=and]
  QueryBenchmark.BenchmarkPrefixSearchDocuments: DefaultJob [FileName=100MB.txt, Query=and]
  QueryBenchmark.BenchmarkNormalSearchDocuments: DefaultJob [FileName=100MB.txt, Query=and]
  QueryBenchmark.BenchmarkPrefixSearch: DefaultJob [FileName=100MB.txt, Query=bread]
  QueryBenchmark.BenchmarkPrefixSearchDocuments: DefaultJob [FileName=100MB.txt, Query=bread]
  QueryBenchmark.BenchmarkNormalSearchDocuments: DefaultJob [FileName=100MB.txt, Query=bread]
  QueryBenchmark.BenchmarkPrefixSearch: DefaultJob [FileName=100MB.txt, Query=cat]
  QueryBenchmark.BenchmarkPrefixSearchDocuments: DefaultJob [FileName=100MB.txt, Query=cat]
  QueryBenchmark.BenchmarkNormalSearchDocuments: DefaultJob [FileName=100MB.txt, Query=cat]
  QueryBenchmark.BenchmarkPrefixSearch: DefaultJob [FileName=100MB.txt, Query=or]
  QueryBenchmark.BenchmarkPrefixSearchDocuments: DefaultJob [FileName=100MB.txt, Query=or]
  QueryBenchmark.BenchmarkNormalSearchDocuments: DefaultJob [FileName=100MB.txt, Query=or]
  QueryBenchmark.BenchmarkPrefixSearch: DefaultJob [FileName=10MB.txt, Query=and]
  QueryBenchmark.BenchmarkPrefixSearchDocuments: DefaultJob [FileName=10MB.txt, Query=and]
  QueryBenchmark.BenchmarkNormalSearchDocuments: DefaultJob [FileName=10MB.txt, Query=and]
  QueryBenchmark.BenchmarkPrefixSearch: DefaultJob [FileName=10MB.txt, Query=bread]
  QueryBenchmark.BenchmarkPrefixSearchDocuments: DefaultJob [FileName=10MB.txt, Query=bread]
  QueryBenchmark.BenchmarkNormalSearchDocuments: DefaultJob [FileName=10MB.txt, Query=bread]
  QueryBenchmark.BenchmarkPrefixSearch: DefaultJob [FileName=10MB.txt, Query=cat]
  QueryBenchmark.BenchmarkPrefixSearchDocuments: DefaultJob [FileName=10MB.txt, Query=cat]
  QueryBenchmark.BenchmarkNormalSearchDocuments: DefaultJob [FileName=10MB.txt, Query=cat]
  QueryBenchmark.BenchmarkPrefixSearch: DefaultJob [FileName=10MB.txt, Query=or]
  QueryBenchmark.BenchmarkPrefixSearchDocuments: DefaultJob [FileName=10MB.txt, Query=or]
  QueryBenchmark.BenchmarkNormalSearchDocuments: DefaultJob [FileName=10MB.txt, Query=or]
  QueryBenchmark.BenchmarkPrefixSearch: DefaultJob [FileName=1MB.txt, Query=and]
  QueryBenchmark.BenchmarkPrefixSearchDocuments: DefaultJob [FileName=1MB.txt, Query=and]
  QueryBenchmark.BenchmarkNormalSearchDocuments: DefaultJob [FileName=1MB.txt, Query=and]
  QueryBenchmark.BenchmarkPrefixSearch: DefaultJob [FileName=1MB.txt, Query=bread]
  QueryBenchmark.BenchmarkPrefixSearchDocuments: DefaultJob [FileName=1MB.txt, Query=bread]
  QueryBenchmark.BenchmarkNormalSearchDocuments: DefaultJob [FileName=1MB.txt, Query=bread]
  QueryBenchmark.BenchmarkPrefixSearch: DefaultJob [FileName=1MB.txt, Query=cat]
  QueryBenchmark.BenchmarkPrefixSearchDocuments: DefaultJob [FileName=1MB.txt, Query=cat]
  QueryBenchmark.BenchmarkNormalSearchDocuments: DefaultJob [FileName=1MB.txt, Query=cat]
  QueryBenchmark.BenchmarkPrefixSearch: DefaultJob [FileName=1MB.txt, Query=or]
  QueryBenchmark.BenchmarkPrefixSearchDocuments: DefaultJob [FileName=1MB.txt, Query=or]
  QueryBenchmark.BenchmarkNormalSearchDocuments: DefaultJob [FileName=1MB.txt, Query=or]
  QueryBenchmark.BenchmarkPrefixSearch: DefaultJob [FileName=20MB.txt, Query=and]
  QueryBenchmark.BenchmarkPrefixSearchDocuments: DefaultJob [FileName=20MB.txt, Query=and]
  QueryBenchmark.BenchmarkNormalSearchDocuments: DefaultJob [FileName=20MB.txt, Query=and]
  QueryBenchmark.BenchmarkPrefixSearch: DefaultJob [FileName=20MB.txt, Query=bread]
  QueryBenchmark.BenchmarkPrefixSearchDocuments: DefaultJob [FileName=20MB.txt, Query=bread]
  QueryBenchmark.BenchmarkNormalSearchDocuments: DefaultJob [FileName=20MB.txt, Query=bread]
  QueryBenchmark.BenchmarkPrefixSearch: DefaultJob [FileName=20MB.txt, Query=cat]
  QueryBenchmark.BenchmarkPrefixSearchDocuments: DefaultJob [FileName=20MB.txt, Query=cat]
  QueryBenchmark.BenchmarkNormalSearchDocuments: DefaultJob [FileName=20MB.txt, Query=cat]
  QueryBenchmark.BenchmarkPrefixSearch: DefaultJob [FileName=20MB.txt, Query=or]
  QueryBenchmark.BenchmarkPrefixSearchDocuments: DefaultJob [FileName=20MB.txt, Query=or]
  QueryBenchmark.BenchmarkNormalSearchDocuments: DefaultJob [FileName=20MB.txt, Query=or]
  QueryBenchmark.BenchmarkPrefixSearch: DefaultJob [FileName=2MB.txt, Query=and]
  QueryBenchmark.BenchmarkPrefixSearchDocuments: DefaultJob [FileName=2MB.txt, Query=and]
  QueryBenchmark.BenchmarkNormalSearchDocuments: DefaultJob [FileName=2MB.txt, Query=and]
  QueryBenchmark.BenchmarkPrefixSearch: DefaultJob [FileName=2MB.txt, Query=bread]
  QueryBenchmark.BenchmarkPrefixSearchDocuments: DefaultJob [FileName=2MB.txt, Query=bread]
  QueryBenchmark.BenchmarkNormalSearchDocuments: DefaultJob [FileName=2MB.txt, Query=bread]
  QueryBenchmark.BenchmarkPrefixSearch: DefaultJob [FileName=2MB.txt, Query=cat]
  QueryBenchmark.BenchmarkPrefixSearchDocuments: DefaultJob [FileName=2MB.txt, Query=cat]
  QueryBenchmark.BenchmarkNormalSearchDocuments: DefaultJob [FileName=2MB.txt, Query=cat]
  QueryBenchmark.BenchmarkPrefixSearch: DefaultJob [FileName=2MB.txt, Query=or]
  QueryBenchmark.BenchmarkPrefixSearchDocuments: DefaultJob [FileName=2MB.txt, Query=or]
  QueryBenchmark.BenchmarkNormalSearchDocuments: DefaultJob [FileName=2MB.txt, Query=or]
  QueryBenchmark.BenchmarkPrefixSearch: DefaultJob [FileName=50MB.txt, Query=and]
  QueryBenchmark.BenchmarkPrefixSearchDocuments: DefaultJob [FileName=50MB.txt, Query=and]
  QueryBenchmark.BenchmarkNormalSearchDocuments: DefaultJob [FileName=50MB.txt, Query=and]
  QueryBenchmark.BenchmarkPrefixSearch: DefaultJob [FileName=50MB.txt, Query=bread]
  QueryBenchmark.BenchmarkPrefixSearchDocuments: DefaultJob [FileName=50MB.txt, Query=bread]
  QueryBenchmark.BenchmarkNormalSearchDocuments: DefaultJob [FileName=50MB.txt, Query=bread]
  QueryBenchmark.BenchmarkPrefixSearch: DefaultJob [FileName=50MB.txt, Query=cat]
  QueryBenchmark.BenchmarkPrefixSearchDocuments: DefaultJob [FileName=50MB.txt, Query=cat]
  QueryBenchmark.BenchmarkNormalSearchDocuments: DefaultJob [FileName=50MB.txt, Query=cat]
  QueryBenchmark.BenchmarkPrefixSearch: DefaultJob [FileName=50MB.txt, Query=or]
  QueryBenchmark.BenchmarkPrefixSearchDocuments: DefaultJob [FileName=50MB.txt, Query=or]
  QueryBenchmark.BenchmarkNormalSearchDocuments: DefaultJob [FileName=50MB.txt, Query=or]
  QueryBenchmark.BenchmarkPrefixSearch: DefaultJob [FileName=5MB.txt, Query=and]
  QueryBenchmark.BenchmarkPrefixSearchDocuments: DefaultJob [FileName=5MB.txt, Query=and]
  QueryBenchmark.BenchmarkNormalSearchDocuments: DefaultJob [FileName=5MB.txt, Query=and]
  QueryBenchmark.BenchmarkPrefixSearch: DefaultJob [FileName=5MB.txt, Query=bread]
  QueryBenchmark.BenchmarkPrefixSearchDocuments: DefaultJob [FileName=5MB.txt, Query=bread]
  QueryBenchmark.BenchmarkNormalSearchDocuments: DefaultJob [FileName=5MB.txt, Query=bread]
  QueryBenchmark.BenchmarkPrefixSearch: DefaultJob [FileName=5MB.txt, Query=cat]
  QueryBenchmark.BenchmarkPrefixSearchDocuments: DefaultJob [FileName=5MB.txt, Query=cat]
  QueryBenchmark.BenchmarkNormalSearchDocuments: DefaultJob [FileName=5MB.txt, Query=cat]
  QueryBenchmark.BenchmarkPrefixSearch: DefaultJob [FileName=5MB.txt, Query=or]
  QueryBenchmark.BenchmarkPrefixSearchDocuments: DefaultJob [FileName=5MB.txt, Query=or]
  QueryBenchmark.BenchmarkNormalSearchDocuments: DefaultJob [FileName=5MB.txt, Query=or]
