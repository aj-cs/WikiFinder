using System.Text.RegularExpressions;

namespace SearchEngineProject.Analysis.Interfaces;

public interface ITokenizer
{
    IEnumerable<Token> Tokenize(string text);
}



