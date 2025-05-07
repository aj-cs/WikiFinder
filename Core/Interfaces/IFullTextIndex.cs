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
}
