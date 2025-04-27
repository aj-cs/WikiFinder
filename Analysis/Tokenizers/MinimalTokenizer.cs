using System.Runtime.CompilerServices;
using SearchEngineProject.Analysis.Interfaces;

namespace SearchEngineProject.Analysis.Tokenizers;

public class MinimalTokenizer : ITokenizer
{

    public IEnumerable<Token> Tokenize(string text)
    {
        // normalise 
        var normalized = text.Normalize(System.Text.NormalizationForm.FormC)
                   .ToLowerInvariant();

        var span = normalized.AsSpan();
        int position = 0;
        int i = 0;
        int len = span.Length;

        while (i < len)
        {
            // skip till we find a letter/digit
            while (i < len && !IsWordChar(span[i]))
            {
                i++;
            }
            if (i >= len)
            {
                yield break;
            }

            int start = i;

            //consume word (lettrs or digits)
            while (i < len && IsWordChar(span[i]))
            {
                i++;
            }
            int end = i;

            // slice otu the token, we allocate ONE string for every token
            string term = new string(span[start..end]);
            yield return new Token
            {
                Term = term,
                Position = position++,
                StartOffset = start,
                EndOffset = end
            };
        }

    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] // tell comp to aggresively inline since we use it often
    private static bool IsWordChar(char c) => char.IsLetter(c) || char.IsDigit(c);
}
