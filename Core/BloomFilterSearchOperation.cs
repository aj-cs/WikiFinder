using System.Threading.Tasks;
using SearchEngine.Core.Interfaces;

namespace SearchEngine.Core;

public class BloomFilterSearchOperation : ISearchOperation
{
    public string Name => "bloom";
    private readonly IBloomFilter _bloomFilter;

    public BloomFilterSearchOperation(IBloomFilter bloomFilter)
    {
        _bloomFilter = bloomFilter;
    }

    public Task<object> SearchAsync(string query)
    {
        // return true if the term might exist, false if it definitely doesn't
        return Task.FromResult<object>(_bloomFilter.MightContain(query));
    }
} 