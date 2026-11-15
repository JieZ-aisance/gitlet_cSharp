using System.Text.Json;
using System.Text;
using System.Globalization;
using Microsoft.VisualBasic;


namespace Gitlet.Commands;

public class CommandMerge : ICommand
{
    
    public int ExpectedArgs => 2;

    public string Execute(GitletContexte contexte, string[] args)
    {
        var givenBranchName = args[1];
        
        var branchCurrHash = contexte.Repository.GetCommitHash();
        
        var branchCurr = contexte.Repository.ReadCommit();
        
        var branchCurrFiles = branchCurr.Files;
        
        var branchCurrFilesNames = branchCurrFiles.Keys;
        
        var branchGivenPath = contexte.Paths.BranchFile(givenBranchName);
    
        if (!contexte.Objects.Exists("branch", branchGivenPath))
        {
            throw new GitletException(ExceptionMessages.BranchNoExists);
        }


        var branchGivenHash = File.ReadAllText(branchGivenPath).Trim();
   
        var branchGiven = contexte.Repository.ReadCommit(branchGivenHash);
        
        var branchGivenFiles = branchGiven.Files;
        
        var branchGivenFilesNames = branchGivenFiles.Keys;
        
        if (contexte.Repository.CurrentBranch() == givenBranchName)
        {
            throw new GitletException(ExceptionMessages.CannotMergeWithItself);
        }
        
        if (contexte.Repository.ReadStaging().Additions.Count != 0 || contexte.Repository.ReadStaging().Removals.Count != 0)
        {

            throw new GitletException(ExceptionMessages.UncommittedChanges);
        }

        
        var splitPoint = contexte.Repository.FindSplitPointBfs(branchCurrHash, branchGivenHash);
        
        var splitPointCommit = contexte.Repository.ReadCommit(splitPoint);
        
        var splitPointFiles = splitPointCommit.Files;
        
        var splitPointFilesNames = splitPointFiles.Keys;
        
        if (branchGivenHash == splitPoint)
        {
            throw new GitletException(ExceptionMessages.GivenIsAncestor);
        }
        

        if (branchCurrHash == splitPoint)
        {
            
            new CommandReset().Execute(contexte, ["reset", branchGivenHash]);
            
            throw new GitletException(ExceptionMessages.FastForwarded);
            
        }

        foreach (var name in branchGivenFilesNames)
        {
            var path = Path.Combine(contexte.WorkingDirectory, name);
            
            if (File.Exists(path) && !branchCurrFiles.ContainsKey(name))
            {
                
                throw new GitletException(ExceptionMessages.UntrackedFileInTheWay);
                
            }
        }
        
      
        var files = new SortedDictionary<string, string>(branchCurr.Files);
        
        var hasConflits = false;
        
        //在split点存在的文件
        
        //拿splitpointfiles出来遍历
        foreach (var name in splitPointFilesNames)
        {
            //在curr和given中同时存在
            if (branchCurrFilesNames.Contains(name) && branchGivenFilesNames.Contains(name))
            {
                
                //取给定分支的版本
                var fileGivenContent = branchGivenFiles[name];
                var fileCurrContent = branchCurrFiles[name];
                var fileSplitPointContent = splitPointFiles[name];
                
                
                if ( fileGivenContent != fileSplitPointContent  && fileCurrContent == fileSplitPointContent )
                {
                    //规则1:在给定分支中被修改，在当前分支中没有被修改的，应该改为他们在给定分支中的版本，
                    var content = contexte.Objects.Read("blob", fileGivenContent);
                    File.WriteAllBytes(Path.Combine(contexte.WorkingDirectory, name), content);
                    files[name] = fileGivenContent;
                }

                if (fileGivenContent == fileSplitPointContent && fileCurrContent != fileSplitPointContent)
                {
                    //规则2:在当前分支被修改、给定分支没改 → 保持当前分支的版本。是以当前分支为准
                    var content =contexte.Objects.Read("blob", fileCurrContent);
                    File.WriteAllBytes(Path.Combine(contexte.WorkingDirectory, name), content);
                    files[name] = fileCurrContent;
                }

                if (fileGivenContent != fileSplitPointContent
                    && fileCurrContent != fileSplitPointContent 
                    && fileGivenContent != fileCurrContent)
                {
                    //冲突conflit
                    //规则3:给定和当前都改了，而且改的不一样
                    contexte.Repository.WriteConflict(name, fileCurrContent, fileGivenContent,files);
                    hasConflits = true;
                    //throw new GitletException(ExceptionMessages.MergeConflict);
                    
                }
                
            }
            
            //在curr中存在，但是given中被删除
            if (branchCurrFilesNames.Contains(name) && !branchGivenFilesNames.Contains(name))
            {
                var fileCurrContent = branchCurrFiles[name];
                var fileSplitPointContent = splitPointFiles[name];
                
                //规则4:在当前分支没有被修改，在给定分支中不存在的文件，应该被删除
                if (fileCurrContent == fileSplitPointContent)
                {
                    // 规则 6:当前没改,给定删了 → 删除并取消跟踪
                    var path = Path.Combine(contexte.WorkingDirectory, name);
                    if (File.Exists(path))
                    {
                        File.Delete(path);
                    }
                    files.Remove(name);
                }
                
            }
            
            //在curr中被删除，但是在given中仍然存在
            if (!branchCurrFilesNames.Contains(name) && branchGivenFilesNames.Contains(name))
            {
                var fileGivenContent = branchGivenFiles[name];
   
                var fileSplitPointContent = splitPointFiles[name];
                
                if (fileGivenContent != fileSplitPointContent)
                {
                 
                    contexte.Repository.WriteConflict(name, null, fileGivenContent, files);    // 当前那边是 null
                    hasConflits = true;
                    
                    //throw new GitletException(ExceptionMessages.MergeConflict);
                }
            
            }
            
        }
        
        // //在split点不存在的文件
        //
        // foreach (var name in branchCurrFilesNames)
        // {
        //     //规则1:只在当前分支存在的文件，保持原样
        //     if (!splitPointFilesNames.Contains(name) && !branchGivenFilesNames.Contains(name))
        //     {
        //         continue;
        //     }
        // }

        
        //规则2:只在给定分支存在的文件，被检查出并暂存，进入staging，不进commit
        foreach (var name in branchGivenFilesNames)
        {
            //规则2:只在给定分支存在的文件，被检查出并暂存，进入staging，不进commit
            if (!splitPointFilesNames.Contains(name) && !branchCurrFilesNames.Contains(name))
            {
                var blobHash = branchGivenFiles[name];
                var content = contexte.Objects.Read("blob", blobHash);
                File.WriteAllBytes(Path.Combine(contexte.WorkingDirectory, name), content);
                files[name] = blobHash;
            }
        }
        
        //split 没有、当前和给定都有,而且内容不同:
        foreach (var name in branchGivenFilesNames)
        {

            if (!splitPointFilesNames.Contains(name) && branchGivenFilesNames.Contains(name))
            {

                if (!splitPointFiles.ContainsKey(name)
                    && branchCurrFiles.TryGetValue(name, out var curr)
                    && curr != branchGivenFiles[name])
                {
                    if (branchGivenFiles[name]!= branchCurrFiles[name])
                    {
                        //冲突conflit
                       
                        contexte.Repository.WriteConflict(name, branchCurrFiles[name], branchGivenFiles[name], files);
                        hasConflits = true;
                        //throw new GitletException(ExceptionMessages.MergeConflict);
                        
                    
                    }
                    
                }
               
            }
            
        }
        
       
        //create merge commit
        // 1. 造 commit 对象
        
        var currentBranchName = contexte.Repository.CurrentBranch();
        
        var mergeCommit = new Commit
        {
            Message = $"Merged {givenBranchName} into {currentBranchName}.",
            TimeStamp = DateTimeOffset.Now,
            ParentHashes = [branchCurrHash, branchGivenHash],
            Files = files
        };

        // 2. 序列化 → 字节 → 写进 objects,得到这个 commit 的哈希
        var json = JsonSerializer.Serialize(mergeCommit);
        var bytes = Encoding.UTF8.GetBytes(json);
        var mergeCommitHash = contexte.Objects.Write("commit", bytes);

        // 3. 当前分支指向这个新 commit
        File.WriteAllText(contexte.Paths.BranchFile(currentBranchName), mergeCommitHash);
        
        
        //conflit最后抛异常
        if (hasConflits)
        {
            throw new GitletException(ExceptionMessages.MergeConflict);
        }
        
        return $"{givenBranchName} merged into {currentBranchName} successfully." ;
        
    }
    
  
}