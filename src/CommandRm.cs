using System.Text.Json;
namespace Gitlet.Commands;

public class CommandRm:ICommand
{
    public int ExpectedArgs => 2;
   
    public string Execute(GitletContexte contexte, string[] args)
    {
        var filePath =  args[1];
        
        var staging = contexte.Repository.ReadStaging();
        
        var commitCurr = contexte.Repository.ReadCommit();
        
        var isStaged = staging.Additions.ContainsKey(filePath);
        
        var isCommitted = commitCurr.Files.ContainsKey(filePath);
        
        if (!isStaged && !isCommitted)
        {
            throw new GitletException(ExceptionMessages.NoReasonToRemove);
        }

        if (isStaged)
        {
            staging.Additions.Remove(filePath);
        }

        if (isCommitted)
        {
            staging.Removals.Add(filePath);

            string fullPath = Path.Combine(contexte.WorkingDirectory, filePath);
            
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }
        }
        
        string json = JsonSerializer.Serialize(staging);
        
        File.WriteAllText(contexte.Paths.StagingFile, json);
        
        return $"{filePath} removed";

    }
}