using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using SearchEngineProject.Analysis;
using SearchEngineProject.Persistence.Entities;
namespace SearchEngineProject.Persistence;

public class DocumentTokenRepository
{
    private readonly SearchEngineContext _context;

    public DocumentTokenRepository(SearchEngineContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Insert all tokens for a given document in one batch.
    /// </summary>
    public async Task InsertManyAsync(int docId, IEnumerable<Token> tokens)
    {
        var entities = tokens
            .Select(tok => new DocumentTokenEntity
            {
                DocumentId = docId,
                Term = tok.Term,
                Position = tok.Position,
                StartOffset = tok.StartOffset,
                EndOffset = tok.EndOffset
            });
        await _context.DocumentTokens.AddRangeAsync(entities);
        await _context.SaveChangesAsync();
    }
    /// <summary>
    /// Retrieve all tokens for the given docId, in ascending position order.
    /// </summary>
    public async Task<List<Token>> GetByDocIdAsync(int docId)
    {
        return await _context.DocumentTokens
                             .AsNoTracking()
                             .Where(t => t.DocumentId == docId)
                             .OrderBy(t => t.Position)
                             .Select(t => new Token
                             {
                                 Term = t.Term,
                                 Position = t.Position,
                                 StartOffset = t.StartOffset,
                                 EndOffset = t.EndOffset
                             })
                            .ToListAsync();
    }

    ///<summary>
    ///Delete all token rows for the given document.
    ///<summary/>
    public async Task DeleteByDocIdAsync(int docId)
    {
        var tokens = await _context.DocumentTokens
                                   .Where(t => t.DocumentId == docId)
                                   .ToListAsync();
        if (tokens.Count > 0)
        {
            _context.DocumentTokens.RemoveRange(tokens);
            await _context.SaveChangesAsync();
        }
    }

}
