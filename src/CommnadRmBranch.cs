namespace Gitlet.Commands;

public sealed class CommnadRmBranch:ICommand
{
    public int ExpectedArgs => 2;

    public string Execute(GitletContexte contexte, string[] args)
    {
        var branchName = args[1];
    
        if (!File.Exists(contexte.Paths.BranchFile(branchName)))
        {
            throw new GitletException(ExceptionMessages.BranchNoExists);

        }

        if (branchName == contexte.State.CurrentBranch())
        {
            throw new GitletException(ExceptionMessages.DeleteCurrentBranchForbiden);
        }

        //sinon
        File.Delete(contexte.Paths.BranchFile(branchName));
  
        return $"Branch deleted: \" + {branchName}";
    }
    
}