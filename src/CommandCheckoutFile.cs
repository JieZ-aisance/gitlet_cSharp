using System.Text.Json;

namespace Gitlet.Commands;

public sealed class CommandCheckoutFile :ICommand
{
    public int ExpectedArgs => 3;

    public string Execute(GitletContexte contexte, string[] args)
    {
        var fileName = args[2];
        
        var commitCurr = contexte.Repository.ReadCommit();
        
        if (!commitCurr.Files.TryGetValue(fileName, out string blobHash))
        {
            throw new GitletException(ExceptionMessages.FileDoesNotExist);
        }

        byte[] content = contexte.Objects.Read("blob", blobHash);
        
        File.WriteAllBytes(Path.Combine(contexte.WorkingDirectory, fileName), content);

        return $"{fileName} has been restored to its version in HEAD.";
        
    }
}