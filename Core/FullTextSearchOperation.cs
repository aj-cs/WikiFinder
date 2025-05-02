using System.Threading.Tasks;
using SearchEngine.Core.Interfaces;
using System.Collections.Generic;

namespace SearchEngine.Core;

public class FullTextSearchOperation : ISearchOperation
{
    public string Name => "fulltext";
    private readonly IFullTextIndex _index;

    public FullTextSearchOperation(IFullTextIndex index)
    {
        _index = index;
    }

    public Task<object> SearchAsync(string query)
    {
        // Check if it's a phrase search (contains spaces)
        if (query.Contains(' '))
        {
            return Task.FromResult<object>(_index.PhraseSearch(query));
        }
        
        // Check if it's a boolean search (contains && or ||)
        if (query.Contains("&&") || query.Contains("||"))
        {
            return Task.FromResult<object>(_index.BooleanSearch(query));
        }
        
        // Check if it's a ranked search (ends with #)
        if (query.EndsWith('#'))
        {
            return Task.FromResult<object>(_index.RankedSearch(query));
        }
        
        // Default to exact search
        return Task.FromResult<object>(_index.ExactSearch(query));
    }
} 