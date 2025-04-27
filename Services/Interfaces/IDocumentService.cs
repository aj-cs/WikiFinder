using System.Collections.Generic;
using System.Threading.Tasks;
using SearchEngineProject.Analysis;
using SearchEngineProject.Persistence.Entities;

namespace SearchEngineProject.Services.Interfaces;

public interface IDocumentService
{
    Task<int> CreateAsync(string title);
    Task<bool> DeleteAsync(int docId);
    Task<string> GetTitleAsync(int docId);
    Task<IReadOnlyList<DocumentEntity>> GetAllAsync();

    Task InsertTokensAsync(int docId, IEnumerable<Token> tokens);
    Task DeleteTokensAsync(int docId);
    Task<List<Token>> GetTokensAsync(int docId);
}
