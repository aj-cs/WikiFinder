namespace SearchEngineProject.Core.Interfaces;

public interface IFullTextIndex
{
    ///<summary>
    ///Boolean search: ("foo AND bar") → docIds
    ///</summary>
    List<int> BooleanSearch(string booleanQuery);

    ///<summary>
    ///Phrase search: ("\"quick brown fox\"") → docIds
    ///</summary>
    List<int> PhraseSearch(string phrase);

    ///<summary>
    ///Ranked search: ("fox jumps") → [(docId, score)]
    ///</summary>
    List<(int docId, double score)> RankedSearch(string query);
}
