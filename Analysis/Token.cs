namespace SearchEngineProject.Analysis;

public class Token
{
    public string Term { get; set; } // could set as default! if null warnings get annoying
    public int Position { get; set; }
    public int StartOffset { get; set; }
    public int EndOffset { get; set; }
}
