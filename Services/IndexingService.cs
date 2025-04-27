using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SearchEngineProject.Analysis;
using SearchEngineProject.Core.Interfaces;
using SearchEngineProject.Persistence;
using SearchEngineProject.Persistence.Entities;
using SearchEngineProject.Services.Interfaces;

namespace SearchEngineProject.Services;

public class IndexingService : IIndexingService
{
    private readonly Analyzer _analyzer;
    private readonly IDocumentService _docs;
    private readonly IEnumerable<IExactPrefixIndex> _indexes; //later we adjust this to be not just IExactPrefixIndex

    public IndexingService(Analyzer analyzer, IDocumentService docs, IEnumerable<IExactPrefixIndex> indexes)
    {
        _analyzer = analyzer;
        _docs = docs;
        _indexes = indexes;
    }

    public async Task<int> AddDocumentAsync(string title, string content)
    {
        //persist the metadata
        int docId = await _docs.CreateAsync(title);

        // anlyse then persist the tokens then index it into our structure, just trie for now
        var tokens = _analyzer.Analyze(content)
                              .ToList();
        await _docs.InsertTokensAsync(docId, tokens);

        foreach (var index in _indexes)
        {
            index.AddDocument(docId, tokens);
        }

        return docId;
    }
    public async Task<bool> RemoveDocumentAsync(int docId)
    {
        // load tokens
        var tokens = await _docs.GetTokensAsync(docId);

        // remove from each index then delete persisted tokens + metadada
        foreach (var idx in _indexes)
        {
            idx.RemoveDocument(docId, tokens);
        }

        await _docs.DeleteTokensAsync(docId);
        return await _docs.DeleteAsync(docId);
    }

    public async Task RebuildIndexAsync()
    {
        // clear all indexes
        foreach (var index in _indexes)
        {
            index.Clear();
        }

        // re index every persisted document
        var all = await _docs.GetAllAsync();

        foreach (var doc in all)
        {
            var tokens = await _docs.GetTokensAsync(doc.Id);
            foreach (var index in _indexes)
            {
                index.AddDocument(doc.Id, tokens);
            }
        }
    }

}
