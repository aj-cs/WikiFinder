using System.Collections.Generic;
using System.Threading.Tasks;

namespace SearchEngineProject.Services.Interfaces;

public interface ISearchService
{
    /// <summary>
    /// operation = "exact" | "prefixDocs" | "autocomplete" | any future search name
    /// </summary>
    Task<object> SearchAsync(string operation, string query);
}


