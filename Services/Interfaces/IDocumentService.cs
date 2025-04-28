using System.Collections.Generic;
using System.Threading.Tasks;
using SearchEngineProject.Analysis;
using SearchEngineProject.Persistence.Entities;

namespace SearchEngineProject.Services.Interfaces;

public interface IDocumentService
{
    /// <summary>
    /// Persist a new document, returning its generated ID.
    /// </summary>
    Task<int> CreateAsync(string title);

    /// <summary>
    /// Delete the document metadata; returns true if it existed.
    /// </summary>
    Task<bool> DeleteAsync(int docId);

    /// <summary>
    /// Fetch all documents (ID + Title).
    /// </summary>
    Task<IReadOnlyList<DocumentEntity>> GetAllAsync();

    /// <summary>
    /// Lookup a document’s title by ID.
    /// </summary>
    Task<string> GetTitleAsync(int docId);

    /// <summary>
    /// Upsert one row per distinct term for this document,
    /// serializing all positions into JSON on the DB side.
    /// </summary>
    Task UpsertTermsAsync(int docId, IEnumerable<Token> tokens);

    /// <summary>
    /// Load the set of indexed tokens (one per term) for rebuilding in-memory indexes.
    /// </summary>
    Task<List<Token>> GetIndexedTokensAsync(int docId);

    /// <summary>
    /// Remove all term rows for the given document.
    /// </summary>
    Task DeleteTermsAsync(int docId);
}

