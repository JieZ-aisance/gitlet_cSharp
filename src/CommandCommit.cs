using System.Text;
using System.Text.Json;

namespace Gitlet.Commands;

public sealed class CommandCommit : ICommand
{
    public int ExpectedArgs => 2;

    public string Execute(GitletContexte contexte, string[] args)
    {
        var message = args[1];
 
        if (string.IsNullOrEmpty(message) )
        {
            throw new GitletException(ExceptionMessages.PleaseEnterCommitMessage);
        }

        if (contexte.Repository.ReadStaging().Additions.Count == 0 && contexte.Repository.ReadStaging().Removals.Count == 0)
        {
            throw new GitletException(ExceptionMessages.NoChangesAdded);
        }
        
        var staging = contexte.Repository.ReadStaging();

        var commitParent = contexte.Repository.ReadCommit();
        
        var commitParentHash = contexte.Repository.GetCommitHash();

        var commitParentFiles = new SortedDictionary<string, string>(commitParent.Files);
        
        foreach (var pair in staging.Additions)
        {
            commitParentFiles[pair.Key] = pair.Value;
        }

        foreach (string name in staging.Removals)
        {
            commitParentFiles.Remove(name);
        }

        var newCommit = new Commit
        {
            Message = message,
            TimeStamp = DateTimeOffset.Now,
            ParentHashes = [commitParentHash],
            Files = commitParentFiles
        };
        
        var json = JsonSerializer.Serialize(newCommit);
        
        byte[] bytes = Encoding.UTF8.GetBytes(json);
        
        string commitHash = contexte.Objects.Write("commit", bytes);
        
        var currentBranch = contexte.Repository.CurrentBranch();
        
        File.WriteAllText(contexte.Paths.BranchFile(currentBranch), commitHash);
        
        staging.Additions.Clear();
        
        staging.Removals.Clear();
        
        string stagingJson = JsonSerializer.Serialize(staging);
        
        File.WriteAllText(contexte.Paths.StagingFile, stagingJson);

        return $"Commit successful with message: {message}";
    }
}