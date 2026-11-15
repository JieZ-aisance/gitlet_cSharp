namespace Gitlet.Commands;

public sealed class CommandBranch: ICommand
{
    public int ExpectedArgs => 2;

    public string Execute(GitletContexte contexte, string[] args)
    {
        
        var fileName = args[1];
   
        if (File.Exists(contexte.Paths.BranchFile(fileName)))
        {
            throw new GitletException(ExceptionMessages.BranchAlreadyExists);
        }
   
        var commitCurrHash = contexte.Repository.GetCommitHash();

        File.WriteAllText(contexte.Paths.BranchFile(fileName), commitCurrHash);
 
        return $"{fileName} created";
    }
}