using System.Text.Json;

namespace Gitlet.Commands;

public sealed  class CommandCheckoutBranch :ICommand
{
    public int ExpectedArgs => 2;

    public string Execute(GitletContexte contexte, string[] args)
    {
        var branchName = args[1];
        
        if (!File.Exists(contexte.Paths.BranchFile(branchName)))
        {
            throw new GitletException(ExceptionMessages.BranchNoExists);
        }
        
        var currentBranch = contexte.Repository.CurrentBranch();
        if(branchName == currentBranch)
        {
            throw new GitletException(ExceptionMessages.ALreadyOnTheBranch);
        }
        
        var commitCurr = contexte.Repository.ReadCommit();

        var branchPath = contexte.Paths.BranchFile(branchName);          
        
        var targetHash = File.ReadAllText(branchPath).Trim();    
        
        var target = contexte.Repository.ReadCommit(targetHash);
        
        foreach (var fileName in target.Files.Keys)
        {
            var path = Path.Combine(contexte.WorkingDirectory, fileName);
            
            if (File.Exists(path) && !commitCurr.Files.ContainsKey(fileName))
            {
                throw new GitletException(ExceptionMessages.UntrackedFileInTheWay);
            }
        }
        
        
        var targetFiles = target.Files;
        
        foreach (var pair in targetFiles)
        {
            var path = Path.Combine(contexte.WorkingDirectory, pair.Key);
            
            var blobHash = pair.Value;
            
            byte[] content = contexte.Objects.Read("blob", blobHash);
            
            File.WriteAllBytes(path, content);
        }
        
        foreach (var fileName in commitCurr.Files.Keys)
        {
            if (!target.Files.ContainsKey(fileName))
            {
                var path = Path.Combine(contexte.WorkingDirectory, fileName);
                
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }

        File.WriteAllText(contexte.Paths.HeadFile, $"ref: refs/heads/{branchName}");
        
        Console.WriteLine($"On the branch {branchName} now");

        var staging = contexte.Repository.ReadStaging();
        
        staging.Additions.Clear();
        
        staging.Removals.Clear();
        
        File.WriteAllText(contexte.Paths.StagingFile, JsonSerializer.Serialize(staging));
        
        return $"checkout to branch {branchName}";
        
    }
    
}