namespace Gitlet;

public class GitletPaths
{
    //一个字段:工作目录,从构造函数传入,只读。
    private readonly string _workingDirectory;
    
    //constructor
    public GitletPaths(string workingDirectory)
    {
        _workingDirectory = workingDirectory;
    }
    
    //五个属性，它们的路径在gitlet上拼出来
    public string GitletDir => Path.Combine(_workingDirectory, ".gitlet");
    public string ObjectsDir => Path.Combine(GitletDir, "objects");
    public string RefHeadDirec => Path.Combine(GitletDir, "refs", "heads");
    
    public string HeadFile => Path.Combine(GitletDir, "HEAD");
    public string StagingFile => Path.Combine(GitletDir, "staging");
    
    //ObjectPath(string hash) — 给一个 40 字符的哈希,
    //返回它在 objects 里的完整路径。
    //前 2 个字符是目录名,后 38 个是文件名。
    //你可以用 hash[..2] 和 hash[2..] 取这两段。

    public string ObjectPath(string hash)
    {
        return Path.Combine(ObjectsDir, hash[..2], hash[2..]);
    }
        
    //BranchFile(string branchName) — 给分支名,
    //返回 refs/heads/ 下对应文件的路径。
    public string BranchFile(string branchName)
    {
        return Path.Combine(RefHeadDirec, branchName);
    }
    
    //此处学习
    //Path.Combine 字符串拼接
    
    //对象哈希后存储方式：
    //1，ce013625030ba8dba906f756967f9e9ca394464a
    //路径存储为 ../../ce/013625030ba8dba906f756967f9e9ca394464a
    //如此存储的原因： 1）SHA-1 输出的分布是均匀的
    //2）对几万个对象的哈希可以把它们分在256（16^2）个子目录下，查找的时候先配子目录两位哈希
    
    
}