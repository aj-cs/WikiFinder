SearchEngine.sln
└── SearchEngine/            ← main project
    ├── SearchEngine.csproj
    ├── Program.cs           ← WebHost/React startup
    ├── ISearchTypeIndex.cs            ← defines search APIs
    ├── CombinedIndex.cs     ← builds on one or more IIndex’s
    ├── /Core             ← all of the index implementations that we use
    │   ├── SearchEngine.Core.csproj
    │   ├── TernarySearchTreeIndex.cs <- only one of the tries will be used
    │   ├── CompressedTernarySearchTreeIndex.cs 
    │   ├── CompactTrieIndex.cs
    │   └── InvertedIndex.cs
    ├── /Analysis            ← token + analyzer pipeline
    │   ├── SearchEngine.Analysis.csproj
    │   ├── Token.cs
    │   ├── ITokenizer.cs
    │   ├── StandardTokenizer.cs
    │   ├── ITokenFilter.cs
    │   ├── StopWordFilter.cs <- we should have this
    │   ├── PorterStemFilter.cs <- we should have this
    │   ├── SynonymFilter.cs <- we should have this
    │   ├── ShingleFilter.cs <- example
    │   ├── SoundexFilter.cs <- example
    │   └── Analyzer.cs
    ├── /Persistence          ← EF Core and/or custom repositories
    │   ├── SearchEngine.Persistence.csproj
    │   ├── DocumentRepository.cs       ← Documents (Title, Url,…)
    │   └── DocumentTokensRepository.cs ← Token metadata per doc
    └── /Documents            ← high‐level doc ingestion
        ├── SearchEngine.Documents.csproj
        ├── DocumentManager.cs
        ├── IWikipediaDocumentService.cs
        └── WikipediaDocumentService.cs <- implements IWikipediaDocumentService

