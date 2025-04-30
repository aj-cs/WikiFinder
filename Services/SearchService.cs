using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SearchEngine.Analysis;
using SearchEngine.Core.Interfaces;
using SearchEngine.Persistence;
using SearchEngine.Persistence.Entities;
using SearchEngine.Services.Interfaces;
namespace SearchEngine.Services;

public class SearchService : ISearchService
{
    private readonly IDictionary<string, ISearchOperation> _ops;
    private readonly IDocumentService _docs;

    public SearchService(IEnumerable<ISearchOperation> ops, IDocumentService docs)
    {
        _ops = ops.ToDictionary(
                op => op.Name,
                op => op,
                StringComparer.OrdinalIgnoreCase);
        _docs = docs;
    }

    public async Task<object> SearchAsync(string operation, string query)
    {
        if (!_ops.TryGetValue(operation, out var op))
            throw new InvalidOperationException($"Unknown search operation '{operation}'");

        var raw = await op.SearchAsync(query);

        // if its a List<int> thenmap to titles
        if (raw is List<int> ids)
        {
            var titles = new List<string>(ids.Count);
            foreach (var id in ids)
                titles.Add(await _docs.GetTitleAsync(id));
            return titles;
        }

        // if its List<(string,List<int>)> then we  map doc‐IDs to titles
        if (raw is List<(string word, List<int> ids)> ac)
        {
            var output = new List<(string word, List<string> titles)>(ac.Count);
            foreach (var (word, ids2) in ac)
            {
                var titleList = new List<string>(ids2.Count);
                foreach (var id in ids2)
                    titleList.Add(await _docs.GetTitleAsync(id));
                output.Add((word, titleList));
            }
            return output;
        }

        // Otherwise it's a bool or some other scalar—just return it
        return raw;
    }

}
