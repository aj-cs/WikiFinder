using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SearchEngineProject.Analysis;
using SearchEngineProject.Persistence;
using SearchEngineProject.Persistence.Entities;
using SearchEngineProject.Services.Interfaces;

namespace SearchEngineProject.Services;

public class DocumentService : IDocumentService
{
    private readonly DocumentRepository _docs;
    private readonly DocumentTokenRepository _tokens;

    public DocumentService(DocumentRepository docs, DocumentTokenRepository tokens)
    {
        _docs = docs;
        _tokens = tokens;
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


    public Task InsertTokensAsync(int docId, IEnumerable<Token> tokens)
    {
        return _tokens.InsertManyAsync(docId, tokens);
    }

    public Task DeleteTokensAsync(int docId)
    {
        return _tokens.DeleteByDocIdAsync(docId);
    }

    public Task<List<Token>> GetTokensAsync(int docId)
    {
        return _tokens.GetByDocIdAsync(docId);
    }
}
