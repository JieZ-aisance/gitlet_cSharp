namespace Gitlet;

public class Commit
{
    public string Message { get; set; }
    public List<string> ParentHashes { get; init; }
    public DateTimeOffset TimeStamp { get; init; }
    public SortedDictionary<string, string> Files { get; init; }
}