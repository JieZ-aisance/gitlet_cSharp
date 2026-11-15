using System.Text.Json;

namespace Gitlet.Commands;

public sealed class CommandBranch: ICommand
{
    public int ExpectedArgs => 2;

    public string Execute(GitletContexte contexte, string[] args)
    {
        var fileName = args[1];
        //already exist
        if (File.Exists(contexte.Paths.BranchFile(fileName)))
        {
            throw new GitletException(ExceptionMessages.BranchAlreadyExists);
            
        }
        //sinon
        var commitCurrHash = contexte.State.GetCommitHash();
        //public string RefHeadDirec => Path.Combine(GitletDir, "refs", "heads");
        File.WriteAllText(contexte.Paths.BranchFile(fileName), commitCurrHash);
        //Console.WriteLine("New branch created: " + branchName);
        return $"{fileName} created";
    }
}