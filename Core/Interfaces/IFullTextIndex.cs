namespace SearchEngine.Core.Interfaces;
using SearchEngine.Analysis;
public interface IFullTextIndex
{
    void AddDocument(int docId, IEnumerable<Token> tokens);
    void RemoveDocument(int docId, IEnumerable<Token> tokens);
    void Clear();

    // queries -----------------------------------------------------------------
    List<(int docId, int count)> ExactSearch(string term);          // exact word
    List<(int docId, int count)> PhraseSearch(string phrase);       // "foo bar"
    List<(int docId, int count)> BooleanSearch(string expr);        // foo && bar || baz

    /// <summary>
    /// Search for prefix matches
    /// </summary>
    List<(string word, List<int> docIds)> PrefixSearch(string prefix);

    /// <summary>
    /// Search using boolean operators (naive implementation)
    /// </summary>
    List<(int docId, int count)> BooleanSearchNaive(string expr);
    
    /// <summary>
    /// Gets the position data for a specific term and document
    /// </summary>
    /// <param name="term">The term to get positions for</param>
    /// <param name="docId">The document ID</param>
    /// <returns>List of position values or empty list if term/document not found</returns>
    List<int> GetPositions(string term, int docId);

    /// <summary>
    /// Gets whether delta encoding is enabled for this index
    /// </summary>
    bool IsDeltaEncodingEnabled { get; }
    
    /// <summary>
    /// Sets whether delta encoding is enabled for this index
    /// </summary>
    void SetDeltaEncoding(bool on);
}
