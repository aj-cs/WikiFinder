```

BenchmarkDotNet v0.14.0, Ubuntu 24.04.2 LTS (Noble Numbat)
11th Gen Intel Core i7-11370H 3.30GHz, 1 CPU, 8 logical and 4 physical cores
.NET SDK 8.0.115
  [Host]     : .NET 8.0.15 (8.0.1525.16413), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  DefaultJob : .NET 8.0.15 (8.0.1525.16413), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI


```
| Method                     | FileName  | Mean    | Error    | StdDev   | Gen0       | Gen1      | Allocated |
|--------------------------- |---------- |--------:|---------:|---------:|-----------:|----------:|----------:|
| BenchmarkIndexConstruction | 100MB.txt | 4.941 s | 0.0974 s | 0.1000 s | 15000.0000 | 2000.0000 |  912.3 MB |
