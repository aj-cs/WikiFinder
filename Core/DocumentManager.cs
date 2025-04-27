using SearchEngineProject.Persistence;
using SearchEngineProject.Analysis;
using SearchEngineProject.Core.Interfaces;
// using SearchEngineProject.Core.Interfaces;

namespace SearchEngineProject.Core;

///<summary>
///Responsible for persisting, analysing and indexing documents
///<summary/>
public class DocumentManager
{
    private readonly Analyzer _analyzer;
    private readonly IExactPrefixIndex _trie;
    private readonly DocumentRepository _docRepo;
    private readonly DocumentTokenRepository _tokenRepo;

    public DocumentManager(Analyzer analyzer, IExactPrefixIndex trie, DocumentRepository docRepo, DocumentTokenRepository tokenRepo)
    {
        _analyzer = analyzer;
        _trie = trie;
        _docRepo = docRepo;
        _tokenRepo = tokenRepo;
    }
    /// <summary>
    /// Adds a new document (title + content) to the database and indexes it.
    /// Returns the generated document ID.
    /// </summary>
    public async Task<int> AddDocumentAsync(string title, string content)
    {
        // persist document metadata
        int docId = await _docRepo.InsertAsync(title);

        // analyse content into tokens
        var tokens = _analyzer
            .Analyze(title)
            .ToList();

        // persist tokens for future removal or rebuild
        await _tokenRepo.InsertManyAsync(docId, tokens);

        //register and add into in memory index
        _trie.AddDocument(docId, tokens);

        return docId;
    }

    /// <summary>
    /// Removes a document by ID from both the index and the database.
    /// Returns true if the document existed and was deleted.
    /// </summary>
    public async Task<bool> RemoveDocumentAsync(int docId)
    {
        var tokens = await _tokenRepo.GetByDocIdAsync(docId);
        _trie.RemoveDocument(docId, tokens);
        await _tokenRepo.DeleteByDocIdAsync(docId);
        return await _docRepo.DeleteAsync(docId);
    }
    /// <summary>
    /// Clears and rebuilds the in‐memory index from all persisted documents.
    /// </summary>
    public async Task RebuildIndexAsync()
    {

        var allDocs = await _docRepo.GetAllAsync();
        foreach (var doc in allDocs)
        {
            var tokens = await _tokenRepo.GetByDocIdAsync(doc.Id);
            _trie.AddDocument(doc.Id, tokens);
        }
    }
}
