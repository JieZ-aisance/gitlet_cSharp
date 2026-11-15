using System.Text.Json;
using System.Text;

namespace Gitlet.Commands;

public sealed class CommandInit : ICommand
{
    public int ExpectedArgs => 1;

    public string Execute(GitletContexte contexte, string[] args)
    {
    
        if (Directory.Exists(contexte.Paths.GitletDir))
        {
            
            throw new GitletException(ExceptionMessages.AlreadyInitialized);
            
        }
        
        Directory.CreateDirectory(contexte.Paths.GitletDir);

        Directory.CreateDirectory(contexte.Paths.ObjectsDir);
   
        Directory.CreateDirectory(contexte.Paths.RefHeadDirec);   
        
        var initial = new Commit
        {
            Message = "initial commit",
            
            TimeStamp = new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero),
            
            ParentHashes = [],
            
            Files = []
        };
        
        var json = JsonSerializer.Serialize(initial);
        
        byte[] bytes = Encoding.UTF8.GetBytes(json);
        
        string commitHash = contexte.Objects.Write("commit", bytes);
        
        File.WriteAllText(contexte.Paths.BranchFile("main"), commitHash);
        
        File.WriteAllText(contexte.Paths.HeadFile, "ref: refs/heads/main");
        
        return "project initialised";
        
    }
}
