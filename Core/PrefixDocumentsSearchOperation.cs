using System.Threading.Tasks;
using SearchEngine.Core.Interfaces;
using System.Collections.Generic;

namespace SearchEngine.Core;

public class PrefixDocsSearchOperation : ISearchOperation
{
    public string Name => "prefixDocs";
    private readonly IExactPrefixIndex _trie;
    public PrefixDocsSearchOperation(IExactPrefixIndex trie)
    {
        _trie = trie;
    }


    public Task<object> SearchAsync(string query)
    {
        List<int> ids = _trie.PrefixSearchDocuments(query);
        return Task.FromResult<object>(ids);
    }
}

