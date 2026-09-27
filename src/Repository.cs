using System.Text.Json;
using System.Text;

namespace Gitlet;
//TODO: any imports you need here

/** Represents a gitlet repository.
 *  TODO: It's a good idea to give a description here of what else this Class
 *  does at a high level.
 *
 *  @author TODO
 */
public class Repository
{
    private readonly string _workingDirectory;
    private readonly GitletPaths _paths;
    private readonly ObjectStore _objects;
    //把路径和被存储的数据都视为对象

    public Repository(string workingDirectory)
    {
        _workingDirectory = workingDirectory;
        _paths = new GitletPaths(workingDirectory);
        _objects = new ObjectStore(_paths);
    }
    //init
    public void Init()//Init 不需要返回任何东西——它的效果是在磁盘上建出目录和文件
    {
        //1,如果已经init过了，就报错
        if (Directory.Exists(_paths.GitletDir))// public string GitletDir => Path.Combine(_workingDirectory, ".gitlet");
        {
            throw new GitletException("A Gitlet version-control system already exists in the current directory.");
        }
        
        //2,建目录
        Directory.CreateDirectory(_paths.GitletDir);
        // public string ObjectsDir => Path.Combine(GitletDir, "objects");
        Directory.CreateDirectory(_paths.ObjectsDir);
        //public string RefHeadDirec => Path.Combine(GitletDir, "refs", "heads");
        Directory.CreateDirectory(_paths.RefHeadDirec);   // 顺带建出 refs
        
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
        string commitHash = _objects.Write("commit", bytes);
        
        //5,写入 默认分支refs/heads/master
        File.WriteAllText(_paths.BranchFile("main"), commitHash);
        
        //HEAD
        File.WriteAllText(_paths.HeadFile, "ref: refs/heads/main");
        
    }
    
    //add
    public void Add(string path)
    {
        //get the current commit
        
        
        //
    }
    
    //Helper
    private  Commit ReadCommit()
    {
        //HEAD中存储的是ref: refs/heads/master，是当前分支的string格式的路径
        var head = File.ReadAllText(_paths.HeadFile);
        const string prefix = "ref: refs/heads/";
        string branchCurr = head[prefix.Length..];
        
        //拼路径
        var currentbranche_path = _paths.BranchFile(branchCurr);
        
        //读分支文件,拿到 commit 哈希(string)
        var commitHash = File.ReadAllText(currentbranche_path).Trim();
        
        //commit的哈希字符串换成字节
        var commitCurr = _objects.Read(commitHash);
        //把字节按照utf8编码成json
        var commitCurrJson = Encoding.UTF8.GetString(commitCurr);
        //反序列化，就是把json？
        var commitCurrObj = JsonSerializer.Deserialize<Commit>(commitCurrJson);
        
        return commitCurrObj;
    }
    
}