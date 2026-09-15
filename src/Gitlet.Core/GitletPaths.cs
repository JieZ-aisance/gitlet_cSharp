namespace Gitlet.Core;

public class GitletPaths
{
    //用来计算.gitlet中的各个文件和目录的路径，别的什么都不做
    //对比之前项目中的拼接api的类的做法
    private readonly string _workingDirectory;
    
    //constructor
    public GitletPaths(string workingDirectory)
    {
        _workingDirectory = workingDirectory;
    }
    
    public string WorkingDirectory => _workingDirectory;
    public string GitletDirectory => Path.Combine(_workingDirectory, ".gitlet");
    public string ObjectsDirectory => Path.Combine(GitletDirectory, "objects");
    public string HeadFile => Path.Combine(GitletDirectory, "HEAD");
    
    // public string RefFile => Path.Combine(GitletDirectory, "refs");
    public string StagingFile => Path.Combine(GitletDirectory, "staging");
    

}