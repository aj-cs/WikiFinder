#!/bin/bash

# Build the benchmark project
echo "Building benchmark project..."
dotnet build -c Release

# Function to display usage information
show_usage() {
  echo "Usage: ./run_benchmarks.sh [OPTION]"
  echo "Run benchmarks for the search engine project."
  echo ""
  echo "Options:"
  echo "  --all                 Run all benchmarks (default)"
  echo "  --basic               Run basic benchmarks with smaller file sizes"
  echo "  --search              Run search comparison benchmarks"
  echo "  --prefix-search       Run only prefix search comparison benchmarks"
  echo "  --exact-search        Run only exact search comparison benchmarks"
  echo "  --boolean-search      Run only boolean search comparison benchmarks"
  echo "  --boolean-search-algos Run dedicated boolean search algorithm benchmarks"
  echo "  --phrase-search       Run only phrase search comparison benchmarks"
  echo "  --construction        Run only data structure construction benchmarks"
  echo "  --delta-encoding      Run delta encoding benchmarks"
  echo "  --features            Run features comparison benchmarks (Bloom filter, etc.)"
  echo "  --text-analysis       Run text analysis filters benchmarks"
  echo "  --help                Display this help and exit"
  echo ""
  echo "Examples:"
  echo "  ./run_benchmarks.sh --search       # Run search comparison benchmarks"
  echo "  ./run_benchmarks.sh --basic        # Run benchmarks with only small files"
  echo "  ./run_benchmarks.sh --delta-encoding # Run delta encoding benchmarks"
  echo "  ./run_benchmarks.sh --boolean-search-algos # Run boolean search algorithm benchmarks"
  echo "  ./run_benchmarks.sh --text-analysis # Run text analysis benchmarks"
}

# Parse command-line options
case "$1" in
  --all|"")
    echo "Running all benchmarks..."
    dotnet run -c Release
    ;;
  --basic)
    echo "Running basic benchmarks with smaller files..."
    dotnet run -c Release -- --filter "*" --job short
    ;;
  --search)
    echo "Running search comparison benchmarks..."
    dotnet run -c Release -- --search-comparison
    ;;
  --prefix-search)
    echo "Running prefix search comparison benchmarks..."
    dotnet run -c Release -- --search-comparison --filter "*PrefixSearch*"
    ;;
  --exact-search)
    echo "Running exact search comparison benchmarks..."
    dotnet run -c Release -- --search-comparison --filter "*ExactSearch*"
    ;;
  --boolean-search)
    echo "Running boolean search comparison benchmarks..."
    dotnet run -c Release -- --search-comparison --filter "*BooleanSearch*"
    ;;
  --boolean-search-algos)
    echo "Running dedicated boolean search algorithm benchmarks..."
    dotnet run -c Release -- --boolean-search
    ;;
  --phrase-search)
    echo "Running phrase search comparison benchmarks..."
    dotnet run -c Release -- --search-comparison --filter "*PhraseSearch*"
    ;;
  --construction)
    echo "Running data structure construction benchmarks..."
    dotnet run -c Release -- --filter "*Build*"
    ;;
  --delta-encoding)
    echo "Running delta encoding benchmarks..."
    dotnet run -c Release -- --delta-encoding
    ;;
  --features)
    echo "Running features comparison benchmarks..."
    dotnet run -c Release -- --features-comparison
    ;;
  --text-analysis)
    echo "Running text analysis filters benchmarks..."
    dotnet run -c Release -- --text-analysis
    ;;
  --help)
    show_usage
    exit 0
    ;;
  *)
    echo "Unknown option: $1"
    show_usage
    exit 1
    ;;
esac

echo "Benchmarks complete. Results can be found in the BenchmarkDotNet.Artifacts directory." 