using System.Text.Json;

namespace Gitlet.Commands;

public sealed class CommandReset : ICommand
{
    public int ExpectedArgs => 2;
    

    public string Execute(GitletContexte contexte, string[] args)
    {
        var commitId = args[1];
        
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
                
                var CurrentCommit = contexte.Repository.ReadCommit();
                
                var target = contexte.Repository.ReadCommit(commitHash);
                
                var files = target.Files;
                
                foreach (var  file in files.Keys)
                {
                    
                    var path = Path.Combine(contexte.WorkingDirectory, file);
                    
                    if (File.Exists(path) && !CurrentCommit.Files.ContainsKey(file))
                    {
                        
                        throw new GitletException(ExceptionMessages.UntrackedFileInTheWay);
                        
                    }
                }
                
                foreach (var pair in files)
                {
                    
                    var path = Path.Combine(contexte.WorkingDirectory, pair.Key);
                    
                    var blobHash = pair.Value;

                    byte[] content = contexte.Objects.Read("blob", blobHash);
                    
                    File.WriteAllBytes(Path.Combine(contexte.WorkingDirectory, pair.Key), content);
                    
                }
                
               
                foreach (var file in CurrentCommit.Files.Keys)
                {
                    
                    if (!target.Files.ContainsKey(file))
                    {
                        var path = Path.Combine(contexte.WorkingDirectory, file);
                        
                        if (File.Exists(path))
                        {
                            File.Delete(path);
                        }
                    }
                }

                
                File.WriteAllText(contexte.Paths.BranchFile(contexte.Repository.CurrentBranch()), commitHash);
                
                
                var staging = contexte.Repository.ReadStaging();
                
                staging.Additions.Clear();
                
                staging.Removals.Clear();
                
                File.WriteAllText(contexte.Paths.StagingFile, JsonSerializer.Serialize(staging));
                
                return "";
                
            }
        }

        throw new GitletException(ExceptionMessages.CommitDoesNotExist);
        
    }

}