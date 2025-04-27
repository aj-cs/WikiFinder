using System.Threading.Tasks;
using SearchEngineProject.Core.Interfaces;

namespace SearchEngineProject.Core;

public class ExactSearchOperation : ISearchOperation
{
    public string Name => "exact";
    private readonly IExactPrefixIndex _trie;
    public ExactSearchOperation(IExactPrefixIndex trie){
        _trie = trie;
    }

    public Task<object> SearchAsync(string query)
    {
        bool found = _trie.Search(query);
        return Task.FromResult<object>(found);
    }
}

