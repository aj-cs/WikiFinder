using SearchEngineProject.Analysis.Interfaces;

namespace SearchEngineProject.Analysis.Tokenizers;

public class MinimalTokenizer : ITokenizer
{

    public IEnumerable<Token> Tokenize(string text)
    {
        // normalise 
        var normalized = text.Normalize(System.Text.NormalizationForm.FormC)
                             .ToLowerInvariant();

        int position = 0;
        int i = 0;
        int len = normalized.Length;

        while (i < len)
        {
            // skip till we find a letter/digit
            while (i < len && !IsWordChar(normalized[i]))
            {
                i++;
            }
            if (i >= len)
            {
                yield break;
            }

            int start = i;

            //consume word (lettrs or digits)
            while (i < len && IsWordChar(normalized[i]))
            {
                i++;
            }
            int end = i;

            // slice otu the token, we allocate ONE string for every token
            string term = normalized.Substring(start, end - start);
            yield return new Token
            {
                Term = term,
                Position = position++,
                StartOffset = start,
                EndOffset = end
            };
        }

    }
    private static bool IsWordChar(char c) => char.IsLetter(c) || char.IsDigit(c);
}
