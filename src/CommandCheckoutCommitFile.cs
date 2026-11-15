namespace Gitlet.Commands;

public sealed class CommandCheckoutCommitFile
{
    public int ExpectedArgs => 4;
    
    public string Execute(GitletContexte contexte, string[] args)
    {
        var commitId = args[1];
        
        var fileName = args[3];

        var commitList = new List<string>();
        
        foreach (var eachCommit in Directory.GetDirectories(contexte.Paths.CommitDir))
        {
            var prefixe = Path.GetFileName(eachCommit);
            
            foreach (var file in Directory.GetFiles(eachCommit))
            {
                var hashtoprint = prefixe + Path.GetFileName(file);
                
                commitList.Add(hashtoprint);
            }
        }

        foreach (var commitHash in commitList)
        {
            if (commitHash.StartsWith(commitId))
            {
                var commit = contexte.Repository.ReadCommit(commitHash);

                var Filelist = commit.Files;

                if (!Filelist.TryGetValue(fileName, out string blobHash))
                {
                    throw new GitletException(ExceptionMessages.FileDoesNotExist);
                }

                byte[] content = contexte.Objects.Read("blob", blobHash);
                
                File.WriteAllBytes(Path.Combine(contexte.WorkingDirectory, fileName), content);
                
                return $"{fileName} has been restored to the version in commit {commitId}.";
                
            }

        }
        throw new GitletException(ExceptionMessages.CommitDoesNotExist);
    }
}