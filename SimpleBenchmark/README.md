# Search Engine Benchmarks

This project contains benchmarks for the search engine implementation using BenchmarkDotNet.

## Benchmark Categories

1. **Index Construction Benchmarks** - Compare the construction time of different data structures (InvertedIndex, CompactTrieIndex, BloomFilter)
2. **Search Operation Benchmarks** - Compare the performance of different search operations (Exact, Prefix, Boolean, Phrase)
3. **Delta Encoding Benchmarks** - Evaluate the impact of delta encoding on index construction, search performance and memory usage
4. **Bloom Filter Benchmarks** - Measure the false positive rates and memory usage of Bloom filters with different configurations
5. **Boolean Search Algorithm Benchmarks** - Compare different implementations of boolean search (naive vs. bitset-based)
6. **Text Analysis Benchmarks** - Compare the performance of different text analysis filters (stopwords, stemming, etc.)
7. **BM25 Parameter Tuning Benchmarks** - Evaluate the impact of different BM25 parameters on search performance
8. **Scaling Benchmarks** - Measure how performance scales with increasing document size
9. **Search Comparison Benchmarks** - Direct comparison of Trie vs. InvertedIndex for exact, prefix, boolean, and phrase search operations

## Running the Benchmarks

You can run all benchmarks with:

```bash
./run_benchmarks.sh
```

For specific benchmark types, use one of the following options:

```bash
# Run all benchmarks
./run_benchmarks.sh --all

# Run basic benchmarks with smaller files (faster completion)
./run_benchmarks.sh --basic

# Run search comparison benchmarks
./run_benchmarks.sh --search

# Run only prefix search comparison benchmarks
./run_benchmarks.sh --prefix-search

# Run only exact search comparison benchmarks
./run_benchmarks.sh --exact-search

# Run only boolean search comparison benchmarks
./run_benchmarks.sh --boolean-search

# Run dedicated boolean search algorithm benchmarks
./run_benchmarks.sh --boolean-search-algos

# Run only phrase search comparison benchmarks
./run_benchmarks.sh --phrase-search

# Run only data structure construction benchmarks
./run_benchmarks.sh --construction

# Run delta encoding benchmarks
./run_benchmarks.sh --delta-encoding

# Run features comparison benchmarks (Bloom filter, etc.)
./run_benchmarks.sh --features

# Run text analysis filters benchmarks
./run_benchmarks.sh --text-analysis

# Display help
./run_benchmarks.sh --help
```

You can also run specific benchmarks directly with dotnet:

```bash
dotnet run -c Release -- --delta-encoding
dotnet run -c Release -- --features-comparison
dotnet run -c Release -- --search-comparison
dotnet run -c Release -- --boolean-search
dotnet run -c Release -- --text-analysis
```

## Benchmark Details

### Delta Encoding Benchmarks

These benchmarks compare the performance and memory usage of inverted indexes with and without delta encoding:

- **BuildInvertedIndexWithoutDelta** - Builds an inverted index without delta encoding
- **BuildInvertedIndexWithDelta** - Builds an inverted index with delta encoding
- **SearchInvertedIndexWithoutDelta** - Searches in an inverted index without delta encoding
- **SearchInvertedIndexWithDelta** - Searches in an inverted index with delta encoding
- **MeasureMemoryUsageWithoutDelta** - Measures memory usage of an inverted index without delta encoding
- **MeasureMemoryUsageWithDelta** - Measures memory usage of an inverted index with delta encoding

### Bloom Filter Benchmarks

These benchmarks evaluate Bloom filters with different configurations:

- **BloomFilter_FalsePositiveRate** - Measures false positive rates with different false positive probability settings
- **BloomFilter_MemoryUsage** - Measures memory usage with different false positive probability settings

### Boolean Search Algorithm Benchmarks

These benchmarks compare different implementations of boolean search operations:

- **BooleanSearch_Naive** - Implements boolean search using HashSet operations (intersect, union)
- **BooleanSearch_BitArray** - Implements boolean search using BitArray operations (AND, OR)
- **BooleanSearch_Complex_Naive** - Benchmarks the naive approach with a more complex query (4 terms)
- **BooleanSearch_Complex_BitArray** - Benchmarks the BitArray approach with a more complex query

### Text Analysis Benchmarks

These benchmarks measure the performance of various text analysis filters used in preprocessing text for search:

- **BasicTokenization** - Simple tokenization without any filtering (baseline)
- **StopwordFiltering** - Tokenization with stopword removal
- **AlphanumericFiltering** - Keeps only letters and numbers in tokens
- **MinLengthFiltering** - Filters out tokens shorter than a minimum length
- **StopwordAndAlphanumeric** - Combination of stopword and alphanumeric filtering
- **BasicStemming** - Applies a simple stemming algorithm (suffix removal)
- **CompleteFilteringPipeline** - A full preprocessing pipeline combining all filters

## Search Comparison Benchmarks

The search comparison benchmarks (`SearchOperationsBenchmark` class) directly compare InvertedIndex and Trie data structures on:

1. **Exact Search** - Finding exact matches for common terms like "the", "and", "history", etc.
2. **Prefix Search** - Finding all words starting with prefixes like "th", "an", "co", etc.
3. **Boolean Search** - Finding documents containing terms with boolean operators (AND, OR) like "the && and", "history || of", etc.
4. **Phrase Search** - Finding documents containing exact phrases like "in the", "history of", etc.

These benchmarks are particularly useful for understanding the performance tradeoffs between these data structures. Trie structures typically excel at prefix searches while inverted indices are often better for exact matching. The boolean and phrase search benchmarks help evaluate which data structure performs better for more complex search operations.

## Benchmark Data

The benchmarks use Wikipedia text files of different sizes located in `/home/shierfall/Downloads/texts`.

Available sizes:
- 100KB
- 1MB
- 2MB
- 5MB
- 10MB
- 20MB
- 50MB
- 100MB
- 200MB
- 400MB

## Interpreting Results

After running the benchmarks, results will be available in the `BenchmarkDotNet.Artifacts` directory. This includes:
- HTML reports in `BenchmarkDotNet.Artifacts/results`
- CSV files with raw data
- Memory allocation details when using the MemoryDiagnoser

## Modifying Benchmarks

You can adjust the benchmark parameters by modifying the `[Params]` attributes in the benchmark classes. For example:

```csharp
[Params("WestburyLab.wikicorp.201004_100KB.txt", "WestburyLab.wikicorp.201004_1MB.txt")]
public string FileName { get; set; }
```

You can also adjust warmup and iteration counts in the `BenchmarkConfig` class. 