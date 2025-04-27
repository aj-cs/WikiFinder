using System.Collections.Generic;
using System.Threading.Tasks;
using SearchEngineProject.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace SearchEngineProject.Persistence;


public class DocumentRepository
{
    private readonly SearchEngineContext _context;

    public DocumentRepository(SearchEngineContext context)
    {
        _context = context;
    }
    /// <summary>
    /// Inserts a new document with the given title.
    /// Returns the generated document Id.
    /// </summary>
    public async Task<int> InsertAsync(string title)
    {
        var doc = new DocumentEntity { Title = title };
        _context.Documents.Add(doc);
        await _context.SaveChangesAsync();
        return doc.Id;
    }

    /// <summary>
    /// Deletes the document with the given Id.
    /// Returns true if a row was deleted.
    /// </summary>
    public async Task<bool> DeleteAsync(int docId)
    {
        var doc = await _context.Documents.FindAsync(docId);
        if (doc == null)
        {
            return false;
        }
        _context.Documents.Remove(doc);
        await _context.SaveChangesAsync();
        return true;
    }
    /// <summary>
    /// Retrieves the title for a given document Id, or null if not found.
    /// </summary>
    public async Task<string> GetTitleAsync(int docId)
    {
        var doc = await _context.Documents.FindAsync(docId);
        return doc?.Title; // change this later so Title is never null
    }

    /// <summary>
    /// Returns all documents with their Id and Title
    /// <summary/>
    public async Task<IReadOnlyList<DocumentEntity>> GetAllAsync()
    {
        return await _context.Documents
                             .AsNoTracking()
                             .ToListAsync();
    }
}


