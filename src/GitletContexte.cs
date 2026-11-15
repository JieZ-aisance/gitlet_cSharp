namespace Gitlet;

public class GitletContexte
{
    public string WorkingDirectory { get; }
    public GitletPaths Paths { get; }
    public ObjectStore Objects { get; }
    public Repository Repository { get; }

    public GitletContexte(string workingDirectory)
    {
        WorkingDirectory = workingDirectory;
        Paths = new GitletPaths(workingDirectory);
        Objects = new ObjectStore(Paths);
        Repository = new Repository(Paths, Objects, workingDirectory);
    }
}