using System.Text.Json;

namespace Gitlet.Commands;

public sealed  class CommandCheckoutBranch :ICommand
{
    public int ExpectedArgs => 2;

    public string Execute(GitletContexte contexte, string[] args)
    {
        var branchName = args[1];
        
        //检查输入的分支是否存在
        if (!File.Exists(contexte.Paths.BranchFile(branchName)))
        {
            throw new GitletException(ExceptionMessages.BranchNoExists);
            
        }
        
        //检查分支是不是当前分支
        var currentBranch = contexte.Repository.CurrentBranch();
        if(branchName == currentBranch)
        {
            throw new GitletException(ExceptionMessages.ALreadyOnTheBranch);
        }
        
        
        //检查是否有未跟踪文件
        var commitCurr = contexte.Repository.ReadCommit();
        //计算目标branch的哈希
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
        
        //目标分支的每个文件 → 写进工作目录(覆盖)
        var targetFiles = target.Files;
        foreach (var pair in targetFiles)
        {
            var path = Path.Combine(contexte.WorkingDirectory, pair.Key);
            var blobHash = pair.Value;
            byte[] content = contexte.Objects.Read("blob", blobHash);
            File.WriteAllBytes(path, content);
        }
        
        // 删除当前分支跟踪、目标分支没有的文件 
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
        // HEAD 指向目标分支
        File.WriteAllText(contexte.Paths.HeadFile, $"ref: refs/heads/{branchName}");
        Console.WriteLine($"On the branch {branchName} now");
        // 清空暂存区
        var staging = contexte.Repository.ReadStaging();
        staging.Additions.Clear();
        staging.Removals.Clear();
        File.WriteAllText(contexte.Paths.StagingFile, JsonSerializer.Serialize(staging));
    
        
        
        return $"checkout to branch {branchName}";
    }
    
    

}