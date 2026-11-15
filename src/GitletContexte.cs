namespace Gitlet;

public class GitletContexte
{
    public string WorkingDirectory { get; }
    public GitletPaths Paths { get; }
    public ObjectStore Objects { get; }
    public RepositoryState State { get; }

    public GitletContexte(string workingDirectory)
    {
        WorkingDirectory = workingDirectory;
        Paths = new GitletPaths(workingDirectory);
        Objects = new ObjectStore(Paths);
        State = new RepositoryState(Paths, Objects, workingDirectory);
    }
}