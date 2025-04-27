using SearchEngineProject.Analysis.Interfaces;
using Porter2StemmerStandard;
namespace SearchEngineProject.Analysis.Filters;

public class PorterStemFilter : ITokenFilter
{

    private readonly EnglishPorter2Stemmer _stemmer;
    public PorterStemFilter(EnglishPorter2Stemmer stemmer)
    {
        _stemmer = stemmer ?? throw new ArgumentNullException(nameof(stemmer));
    }
    public IEnumerable<Token> Filter(IEnumerable<Token> input)
    {
        foreach (var tok in input)
        {
            var stemResult = _stemmer.Stem(tok.Term).Value;

            yield return new Token
            {
                Term = stemResult,
                Position = tok.Position,
                StartOffset = tok.StartOffset,
                EndOffset = tok.EndOffset
            };
        }
    }
}
