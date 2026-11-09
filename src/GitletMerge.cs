using System.Text.Json;
using System.Text;
using System.Globalization;
using Microsoft.VisualBasic;


namespace Gitlet;

public class GitletMerge
{
    private readonly string _workingDirectory;
    private readonly GitletPaths _paths;
    private readonly ObjectStore _objects;

    private readonly RepositoryState _state;
    
    public GitletMerge(string workingDirectory)
    {
        _workingDirectory = workingDirectory;
        _paths = new GitletPaths(workingDirectory);
        _objects = new ObjectStore(_paths);
        _state = new RepositoryState(_paths, _objects);
    }
    
    //
    
}