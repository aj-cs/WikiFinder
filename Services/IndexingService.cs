using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SearchEngine.Analysis;
using SearchEngine.Core.Interfaces;
using SearchEngine.Persistence;
using SearchEngine.Persistence.Entities;
using SearchEngine.Services.Interfaces;

namespace SearchEngine.Services;
/// <summary>
/// Responsible for tokenization, persistence (via bulk ops), and in-memory indexing.
/// </summary>
public class IndexingService : IIndexingService
{
    private readonly Analyzer _analyzer;
    private readonly IDocumentService _docs;
    private readonly DocumentTermRepository _terms;
    private readonly IEnumerable<IExactPrefixIndex> _indexes; //later we adjust this to be not just IExactPrefixIndex

    public IndexingService(Analyzer analyzer, IDocumentService docs, DocumentTermRepository terms, IEnumerable<IExactPrefixIndex> indexes)
    {
        _analyzer = analyzer;
        _docs = docs;
        _terms = terms;
        _indexes = indexes;
    }

    public async Task<int> AddDocumentAsync(string title, string content)
    {
        //persist the metadata
        int docId = await _docs.CreateAsync(title);

        // anlyse then persist the tokens then index it into our structure, just trie for now
        var tokens = _analyzer.Analyze(content)
                              .ToList();
        await _terms.BulkUpsertTermsAsync(docId, tokens);

        foreach (var index in _indexes)
        {
            index.AddDocument(docId, tokens);
        }

        return docId;
    }
    public async Task<bool> RemoveDocumentAsync(int docId)
    {
        // load tokens
        var termMap = await _terms.GetByDocumentAsync(docId);
        var tokens = termMap.Keys
                            .Select(term => new Token { Term = term })
                            .ToList();

        // remove from each index then delete persisted tokens + metadada
        foreach (var idx in _indexes)
        {
            idx.RemoveDocument(docId, tokens);
        }

        await _terms.DeleteByDocumentAsync(docId);
        return await _docs.DeleteAsync(docId);
    }

    public async Task RebuildIndexAsync()
    {
        // clear all indexes
        foreach (var index in _indexes)
        {
            index.Clear();
        }
        // bulk load every term row once
        var allTerms = await _terms.LoadAllTermsAsync();

        // group by document then rebuild indexes
        var grouped = allTerms.GroupBy(e => e.DocumentId);
        foreach (var group in grouped)
        {
            int docId = group.Key;

            //we only care bout term text for the trie
            var tokens = group.Select(e => new Token { Term = e.Term })
                            .ToList();
            foreach (var index in _indexes)
            {
                index.AddDocument(docId, tokens);
            }
        }


    }

}
