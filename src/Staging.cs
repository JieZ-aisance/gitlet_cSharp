namespace Gitlet;

public sealed class Staging
{
    public SortedDictionary<string, string> Additions { get; init; }
    public SortedSet<string> Removals { get; init; }
    
}
    
