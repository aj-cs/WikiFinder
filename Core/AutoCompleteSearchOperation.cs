using System.Threading.Tasks;
using SearchEngine.Core.Interfaces;
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
        List<(string word, List<int> ids)> hits = _trie.PrefixSearch(query);
        return Task.FromResult<object>(hits);
    }
}
