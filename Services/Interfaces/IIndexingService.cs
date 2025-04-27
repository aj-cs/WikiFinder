using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SearchEngineProject.Analysis;
using SearchEngineProject.Persistence;
using SearchEngineProject.Persistence.Entities;

namespace SearchEngineProject.Services.Interfaces;

public interface IIndexingService
{
    Task<int> AddDocumentAsync(string title, string content);
    Task<bool> RemoveDocumentAsync(int docId);
    Task RebuildIndexAsync();
}
