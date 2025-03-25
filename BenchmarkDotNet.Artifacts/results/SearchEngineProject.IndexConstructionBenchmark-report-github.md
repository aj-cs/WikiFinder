```

BenchmarkDotNet v0.14.0, Ubuntu 24.04.2 LTS (Noble Numbat)
Intel Core i5-8265U CPU 1.60GHz (Whiskey Lake), 1 CPU, 8 logical and 4 physical cores
.NET SDK 8.0.114
  [Host]     : .NET 8.0.14 (8.0.1425.11118), X64 RyuJIT AVX2
  Job-NRVLDE : .NET 8.0.14 (8.0.1425.11118), X64 RyuJIT AVX2

IterationCount=1  WarmupCount=1  

```
| Method                     | FileName  | Mean      | Error | Gen0    | Allocated |
|--------------------------- |---------- |----------:|------:|--------:|----------:|
| **BenchmarkIndexConstruction** | **100KB.txt** |  **10.39 ms** |    **NA** | **15.6250** |    **2.1 MB** |
| **BenchmarkIndexConstruction** | **1MB.txt**   | **184.60 ms** |    **NA** |       **-** |  **21.36 MB** |
