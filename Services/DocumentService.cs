using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SearchEngine.Analysis;
using SearchEngine.Persistence;
using SearchEngine.Persistence.Entities;
using SearchEngine.Services.Interfaces;

namespace SearchEngine.Services;

public class DocumentService : IDocumentService
{
    private readonly DocumentRepository _docs;
    private readonly DocumentTermRepository _terms;

    public DocumentService(DocumentRepository docs, DocumentTermRepository terms)
    {
        _docs = docs;
        _terms = terms;
    }

    public Task<int> CreateAsync(string title)
    {
        return _docs.InsertAsync(title);
    }

    public Task<bool> DeleteAsync(int docId)
    {
        return _docs.DeleteAsync(docId);
    }

    public Task<string> GetTitleAsync(int docId)
    {
        return _docs.GetTitleAsync(docId);
    }

    public Task<IReadOnlyList<DocumentEntity>> GetAllAsync()
    {
        return _docs.GetAllAsync();
    }
    public Task UpsertTermsAsync(int docId, IEnumerable<Token> tokens)
    {
        return _terms.BulkUpsertTermsAsync(docId, tokens);
    }

    public async Task<List<Token>> GetIndexedTokensAsync(int docId)
    {
        // Load each distinct term's positions, but for indexing we only need the term itself
        var termMap = await _terms.GetByDocumentAsync(docId);
        return termMap.Keys
            .Select(term => new Token
            {
                Term = term,
                Position = 0,
                StartOffset = 0,
                EndOffset = 0
            })
        .ToList();
    }

    public Task DeleteTermsAsync(int docId)
    {
        return _terms.DeleteByDocumentAsync(docId);
    }
}
