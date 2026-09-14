namespace Gitlet.Core;

public class Commit
{
    public string Message { get; init; }
    //用init是因为一旦创建，提交对象不可变
    public DateTimeOffset Timestamp{ get; init;}
    
    //TODO 跟踪它提交了哪些文件，哪些文件是正常被跟踪的
    public Commit Parent { get; init; }
    // public List<string> ParentHashes { get; init; }
    
    //construtor
    public Commit(string message, Commit parent)
    {
        Message = message;
        Parent = parent;
        if (parent == null)
        {
            Timestamp = new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero);
        }
        
    }

    public void commit()
    {
        
    }
    
    
}