using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SearchEngine.Analysis;
using SearchEngine.Persistence;
using SearchEngine.Persistence.Entities;
using SearchEngine.Services.Interfaces;

namespace SearchEngine.Services;

/// <summary>
/// Implementation of IDocumentService
/// </summary>
public class DocumentService : IDocumentService
{
    private readonly SearchEngineContext _context;
    private readonly SearchEngine.Persistence.DocumentTermRepository _terms;
    private readonly ILogger<DocumentService> _logger;
    private readonly DocumentRepository _docs;
    private readonly DocumentCompressionService _compressionService;

    public DocumentService(
        SearchEngineContext context,
        SearchEngine.Persistence.DocumentTermRepository terms,
        ILogger<DocumentService> logger,
        DocumentRepository docs, 
        DocumentCompressionService compressionService)
    {
        _context = context;
        _terms = terms;
        _logger = logger;
        _docs = docs;
        _compressionService = compressionService;
    }

    public Task<int> CreateAsync(string title)
    {
        return _docs.InsertAsync(title);
    }
    
    public async Task<int> CreateWithContentAsync(string title, string content)
    {
        var compressedContent = _compressionService.Compress(content);
        return await _docs.InsertWithContentAsync(title, compressedContent);
    }

    public Task<bool> DeleteAsync(int docId)
    {
        return _docs.DeleteAsync(docId);
    }

    public Task<string> GetTitleAsync(int docId)
    {
        return _docs.GetTitleAsync(docId);
    }
    
    public async Task<string> GetContentAsync(int docId)
    {
        var compressedContent = await _docs.GetCompressedContentAsync(docId);
        return _compressionService.Decompress(compressedContent);
    }
    
    public async Task<string> GetContentByTitleAsync(string title)
    {
        var compressedContent = await _docs.GetCompressedContentByTitleAsync(title);
        return _compressionService.Decompress(compressedContent);
    }
    
    public async Task UpdateContentAsync(int docId, string content)
    {
        var compressedContent = _compressionService.Compress(content);
        await _docs.UpdateContentAsync(docId, compressedContent);
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
        var termMap = await _terms.GetByDocumentAsync(docId);
        var tokens = new List<Token>();
        
        foreach (var kvp in termMap)
        {
            string term = kvp.Key;
            List<int> positions = kvp.Value;
            
            // create a token for each position of this term
            foreach (int position in positions)
            {
                tokens.Add(new Token
                {
                    Term = term,
                    Position = position,
                    StartOffset = 0,  // we don't have this info in the database
                    EndOffset = 0     // we don't have this info in the database
                });
            }
        }
        
        return tokens;
    }

    public Task DeleteTermsAsync(int docId)
    {
        return _terms.DeleteTermsForDocumentAsync(docId);
    }
}
