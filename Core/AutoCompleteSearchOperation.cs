using System.Threading.Tasks;
using SearchEngine.Core.Interfaces;
using System.Collections.Generic;
using System.Linq;
namespace SearchEngine.Core;

public class AutoCompleteSearchOperation : ISearchOperation
{
    public string Name => "autocomplete";
    private readonly IExactPrefixIndex _trie;
    
    public AutoCompleteSearchOperation(IExactPrefixIndex trie)
    {
        _trie = trie;
    }

    public Task<object> SearchAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Task.FromResult<object>(new List<string>());
        }
        
        // Get prefix matches for the query (query is already normalized by SearchService if needed)
        List<(string word, List<int> docIds)> hits = _trie.PrefixSearch(query);
        
        // Extract just the words and sort them appropriately
        var wordCompletions = hits
            .OrderByDescending(h => h.docIds.Count) // First by popularity (document count)
            .ThenBy(h => h.word.Length)             // Then by length (shorter completions first)
            .Select(h => h.word)                    // Extract just the word
            .Distinct()                             // Remove duplicates
            .ToList();
        
        // Return just the list of words
        return Task.FromResult<object>(wordCompletions);
    }
}
