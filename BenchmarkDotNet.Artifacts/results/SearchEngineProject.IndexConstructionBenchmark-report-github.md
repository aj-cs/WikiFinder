```

BenchmarkDotNet v0.14.0, Ubuntu 24.04.2 LTS (Noble Numbat)
Intel Core i5-8265U CPU 1.60GHz (Whiskey Lake), 1 CPU, 8 logical and 4 physical cores
.NET SDK 8.0.114
  [Host]     : .NET 8.0.14 (8.0.1425.11118), X64 RyuJIT AVX2
  DefaultJob : .NET 8.0.14 (8.0.1425.11118), X64 RyuJIT AVX2


```
| Method                     | FileName  | Mean | Error |
|--------------------------- |---------- |-----:|------:|
| **BenchmarkIndexConstruction** | **100KB.txt** |   **NA** |    **NA** |
| **BenchmarkIndexConstruction** | **100MB.txt** |   **NA** |    **NA** |
| **BenchmarkIndexConstruction** | **10MB.txt**  |   **NA** |    **NA** |
| **BenchmarkIndexConstruction** | **1MB.txt**   |   **NA** |    **NA** |
| **BenchmarkIndexConstruction** | **20MB.txt**  |   **NA** |    **NA** |
| **BenchmarkIndexConstruction** | **2MB.txt**   |   **NA** |    **NA** |
| **BenchmarkIndexConstruction** | **50MB.txt**  |   **NA** |    **NA** |
| **BenchmarkIndexConstruction** | **5MB.txt**   |   **NA** |    **NA** |

Benchmarks with issues:
  IndexConstructionBenchmark.BenchmarkIndexConstruction: DefaultJob [FileName=100KB.txt]
  IndexConstructionBenchmark.BenchmarkIndexConstruction: DefaultJob [FileName=100MB.txt]
  IndexConstructionBenchmark.BenchmarkIndexConstruction: DefaultJob [FileName=10MB.txt]
  IndexConstructionBenchmark.BenchmarkIndexConstruction: DefaultJob [FileName=1MB.txt]
  IndexConstructionBenchmark.BenchmarkIndexConstruction: DefaultJob [FileName=20MB.txt]
  IndexConstructionBenchmark.BenchmarkIndexConstruction: DefaultJob [FileName=2MB.txt]
  IndexConstructionBenchmark.BenchmarkIndexConstruction: DefaultJob [FileName=50MB.txt]
  IndexConstructionBenchmark.BenchmarkIndexConstruction: DefaultJob [FileName=5MB.txt]
