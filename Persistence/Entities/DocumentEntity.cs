using SearchEngineProject.Analysis;
namespace SearchEngineProject.Persistence.Entities;

public class DocumentEntity
{
    public int Id { get; set; }
    public string Title { get; set; }
    public ICollection<DocumentTokenEntity> Tokens { get; set; } = new List<DocumentTokenEntity>();
}

