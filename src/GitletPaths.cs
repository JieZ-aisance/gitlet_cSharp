namespace Gitlet;

public class GitletPaths
{
    private readonly string _workingDirectory;
    
    //constructor
    public GitletPaths(string workingDirectory)
    {
        _workingDirectory = workingDirectory;
    }
    
    public string GitletDir => Path.Combine(_workingDirectory, ".gitlet");
    
    public string ObjectsDir => Path.Combine(GitletDir, "objects");

    public string BlobDir => Path.Combine(ObjectsDir, "blobs");
   
    public string CommitDir => Path.Combine(ObjectsDir, "commits");
    
    public string RefHeadDirec => Path.Combine(GitletDir, "refs", "heads");
    
    public string HeadFile => Path.Combine(GitletDir, "HEAD");
    
    public string StagingFile => Path.Combine(GitletDir, "staging");
    


    public string ObjectPath(string type, string hash)
    {
        if (type == "blob")
        {
            return Path.Combine(BlobDir, hash[..2], hash[2..]);
        }
        if (type == "commit")
        {
            return Path.Combine(CommitDir, hash[..2], hash[2..]);
        }
        //add branch type for merge
        if (type == "branch")
        {
            return BranchFile(hash);
        }
        
        throw new ArgumentException($"Unknown object kind: {type}");
    }
        

    public string BranchFile(string branchName)
    {
        return Path.Combine(RefHeadDirec, branchName);
    }
}