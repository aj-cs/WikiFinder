    using System;
    using System.Net.Http;
    using System.Text.Json;
    using System.Threading.Tasks;
    using SearchEngine.Services.Interfaces;

    namespace SearchEngine.Services;

    public class WikipediaService : IWikipediaService
    {
        private readonly IIndexingService _indexingService;
        private readonly HttpClient _httpClient;

        public WikipediaService(IIndexingService indexingService, IHttpClientFactory httpClientFactory)
        {
            _indexingService = indexingService;
            _httpClient = httpClientFactory.CreateClient();
        }

        public async Task AddFromWikipediaAsync(string title)
        {
            string url = $"https://en.wikipedia.org/w/api.php?action=query&prop=revisions&rvprop=content&rvslots=main&formatversion=2&redirects=1&format=json&titles={Uri.EscapeDataString(title)}";
            
            try
            {
                string json = await _httpClient.GetStringAsync(url);
                using JsonDocument doc = JsonDocument.Parse(json);
                var rootElement = doc.RootElement;

                if (rootElement.TryGetProperty("query", out var queryElement) &&
                    queryElement.TryGetProperty("pages", out var pagesElement) &&
                    pagesElement.GetArrayLength() > 0)
                {
                    var page = pagesElement[0];
                    
                    if (page.TryGetProperty("pageid", out var pageIdElement) && pageIdElement.GetInt32() == -1)
                    {
                        Console.WriteLine($"Page '{title}' not found or invalid.");
                        return;
                    }
                    if (page.TryGetProperty("revisions", out var revisionsElement) &&
                        revisionsElement.GetArrayLength() > 0)
                    {
                        var revision = revisionsElement[0];

                        if (revision.TryGetProperty("slots", out var slotsElement) &&
                            slotsElement.TryGetProperty("main", out var mainSlot) &&
                            mainSlot.TryGetProperty("content", out var contentElement))
                        {
                            string content = contentElement.GetString();

                            // Use the existing indexing service to add the document
                            await _indexingService.AddDocumentAsync(title, content);
                            Console.WriteLine($"Added Wikipedia article: {title}");
                        }
                        else
                        {
                            Console.WriteLine($"No content found in main slot for '{title}'.");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"No revisions found for '{title}'.");
                    }
                }
                else
                {
                    Console.WriteLine("No valid 'query'/'pages' data found in the response.");
                }
            }
            catch (Exception e)
            {
                throw new Exception($"Error fetching data from Wikipedia: {e.Message}");
            }
        }
    }
