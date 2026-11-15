using System.Text.Json;
using System.Text;

namespace Gitlet.Commands;

public sealed class CommandInit : ICommand
{
    public int ExpectedArgs => 1;

    public string Execute(GitletContexte contexte, string[] args)
    {
        //1,如果已经init过了，就报错
        if (Directory.Exists(contexte.Paths.GitletDir))// public string GitletDir => Path.Combine(_workingDirectory, ".gitlet");
        {
            throw new GitletException(ExceptionMessages.AlreadyInitialized);
        }
        
        //2,建目录
        Directory.CreateDirectory(contexte.Paths.GitletDir);
        // public string ObjectsDir => Path.Combine(GitletDir, "objects");
        Directory.CreateDirectory(contexte.Paths.ObjectsDir);
        //public string RefHeadDirec => Path.Combine(GitletDir, "refs", "heads");
        Directory.CreateDirectory(contexte.Paths.RefHeadDirec);   // 顺带建出 refs
        
        //3, 初始造commit
        //commit 的逻辑是:读当前 commit,复制它的文件映射,应用暂存区的改动,造一个新 commit,父指向旧的那个。
        var initial = new Commit
        {
            Message = "initial commit",
            TimeStamp = new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero),
            ParentHashes = [],
            Files = []//这里是字典，必须选有序字典
        };
        
        //4，序列化，得到字符串
        var json = JsonSerializer.Serialize(initial);
        
        //字符串转字节,这里是utf8编码，c#的默认编码是utf16，因此需要显示指定utf8
        byte[] bytes = Encoding.UTF8.GetBytes(json);
        
        //交给objectstore
        string commitHash = contexte.Objects.Write("commit", bytes);
        
        //5,写入 默认分支refs/heads/master
        File.WriteAllText(contexte.Paths.BranchFile("main"), commitHash);
        
        //HEAD
        File.WriteAllText(contexte.Paths.HeadFile, "ref: refs/heads/main");

 
        return "project initialised";
    }
}
