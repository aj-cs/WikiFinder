namespace SearchEngine.Core.Interfaces;
using SearchEngine.Analysis;
public interface IFullTextIndex
{
    void AddDocument(int docId, IEnumerable<Token> tokens);
    void RemoveDocument(int docId, IEnumerable<Token> tokens);
    void Clear();

    // queries -----------------------------------------------------------------
    List<int> ExactSearch(string term);                   // exact word
    List<int> PhraseSearch(string phrase);               // "foo bar"
    List<int> BooleanSearch(string expr);                // foo && bar || baz
    List<(int docId, double score)> RankedSearch(string termWithHash); // foo#
}
