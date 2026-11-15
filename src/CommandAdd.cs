using System.Text.Json;

namespace Gitlet.Commands;

// add <file name>
// 一次只能 add 一个文件
public sealed class CommandAdd : ICommand
{
    public int ExpectedArgs => 2;

    public string Execute(GitletContexte contexte, string[] args)
    {
        
        var fileName = args[1];
        
        var fullPath = Path.Combine(contexte.WorkingDirectory, fileName);

        if (!File.Exists(fullPath))
        {
            throw new GitletException(ExceptionMessages.FileDoesNotExist);
        }


        var newHash = contexte.Objects.Write("blob", File.ReadAllBytes(fullPath));

        var commitCurrFiles = contexte.Repository.ReadCommit().Files;
        
        var staging = contexte.Repository.ReadStaging();
        
        staging.Removals.Remove(fileName);

        if (commitCurrFiles.TryGetValue(fileName, out string oldHash) && oldHash == newHash)
        {
    
            staging.Additions.Remove(fileName);
        }
        else
        {
          
            staging.Additions[fileName] = newHash;
        }

        File.WriteAllText(contexte.Paths.StagingFile, JsonSerializer.Serialize(staging));
        
        return $"{fileName} added";
        
    }
}