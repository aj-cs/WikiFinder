namespace SearchEngineProject.Services.Interfaces;

public interface IWikipediaService
{
    Task AddFromWikipediaAsync(string title);
}