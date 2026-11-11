using System.Text.Json;
using System.Text;
using System.Globalization;
using Microsoft.VisualBasic;


namespace Gitlet;

public class GitletMergeScope
{
    private readonly string _workingDirectory;
    private readonly GitletPaths _paths;
    private readonly ObjectStore _objects;

    private readonly RepositoryState _state;
    
    public GitletMergeScope(string workingDirectory)
    {
        _workingDirectory = workingDirectory;
        _paths = new GitletPaths(workingDirectory);
        _objects = new ObjectStore(_paths);
        _state = new RepositoryState(_paths, _objects, workingDirectory);
    }

    public void GitletMerge(string givenBranchName)
    {
        //当前分支
        var branchCurrHash = _state.GetCommitHash();
        
        var branchCurr = _state.ReadCommit();
        var branchCurrFiles = branchCurr.Files;
        
        var branchCurrFilesNames = branchCurrFiles.Keys;
        
       // var branchCurrFilesContents = branchCurrFiles.Values;
        
        //给定分支
        //拼路径
        var branchGivenPath = _paths.BranchFile(givenBranchName);
             
        //如果给定分支不存在，会报错
    
        if (!_objects.Exists("branch", branchGivenPath))
        {
            throw new GitletException(ExceptionMessages.BranchNoExists);
        }


        var branchGivenHash = File.ReadAllText(branchGivenPath).Trim();
   
        var branchGiven = _state.ReadCommit(branchGivenHash);
        var branchGivenFiles = branchGiven.Files;
        var branchGivenFilesNames = branchGivenFiles.Keys;
      
        
        //exceptions
        //public const string CannotMergeWithItself = "Cannot merge a branch with itself.";
        //比较分支名，而不是比较哈希。因为不同的分支，完全可以指向同一个commit
        if (_state.CurrentBranch() == givenBranchName)
        {
            throw new GitletException(ExceptionMessages.CannotMergeWithItself);
        }
        
        //staging area not empty暂存区有文件
        if (_state.ReadStaging().Additions.Count != 0 || _state.ReadStaging().Removals.Count != 0)
        {

            throw new GitletException(ExceptionMessages.UncommittedChanges);
        }

        
        //split point
        var splitPoint = _state.FindSplitPointBfs(branchCurrHash, branchGivenHash);
        
        var splitPointCommit = _state.ReadCommit(splitPoint);
        
        var splitPointFiles = splitPointCommit.Files;
        
        var splitPointFilesNames = splitPointFiles.Keys;
        
        // var splitPointFilesContents = splitPointFiles.Values;
        
        //几种exception
       
        //public const string GivenIsAncestor = "Given branch is an ancestor of the current branch.";
        //给定分支是现在分支的祖先
        if (branchGivenHash == splitPoint)
        {
            throw new GitletException(ExceptionMessages.GivenIsAncestor);
        }
        
     
        // 当前分支是给定分支的祖先:快进，执行reset
        if (branchCurrHash == splitPoint)
        {
            new Repository(_workingDirectory).GitletReset(branchGivenHash);
            throw new GitletException(ExceptionMessages.FastForwarded);
        }
        
        
        //未跟踪文件挡路

        foreach (var name in branchGivenFilesNames)
        {
            var path = Path.Combine(_workingDirectory, name);
            if (File.Exists(path) && !branchCurrFiles.ContainsKey(name))
            {
                throw new GitletException(ExceptionMessages.UntrackedFileInTheWay);
            }
        }
        
        //merge 容器
      
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
                    var content = _objects.Read("blob", fileGivenContent);
                    File.WriteAllBytes(Path.Combine(_workingDirectory, name), content);
                    files[name] = fileGivenContent;
                }

                if (fileGivenContent == fileSplitPointContent && fileCurrContent != fileSplitPointContent)
                {
                    //规则2:在当前分支被修改、给定分支没改 → 保持当前分支的版本。是以当前分支为准
                    var content = _objects.Read("blob", fileCurrContent);
                    File.WriteAllBytes(Path.Combine(_workingDirectory, name), content);
                    files[name] = fileCurrContent;
                }

                if (fileGivenContent != fileSplitPointContent
                    && fileCurrContent != fileSplitPointContent 
                    && fileGivenContent != fileCurrContent)
                {
                    //冲突conflit
                    //规则3:给定和当前都改了，而且改的不一样
                    _state.WriteConflict(name, fileCurrContent, fileGivenContent,files);
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
                    var path = Path.Combine(_workingDirectory, name);
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
                 
                    _state.WriteConflict(name, null, fileGivenContent, files);    // 当前那边是 null
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
                var content = _objects.Read("blob", blobHash);
                File.WriteAllBytes(Path.Combine(_workingDirectory, name), content);
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
                       
                         _state.WriteConflict(name, branchCurrFiles[name], branchGivenFiles[name], files);
                        hasConflits = true;
                        //throw new GitletException(ExceptionMessages.MergeConflict);
                        
                    
                    }
                    
                }
               
            }
            
        }
        
       
        //create merge commit
        // 1. 造 commit 对象
        
        var currentBranchName = _state.CurrentBranch();
        
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
        var mergeCommitHash = _objects.Write("commit", bytes);

        // 3. 当前分支指向这个新 commit
        File.WriteAllText(_paths.BranchFile(currentBranchName), mergeCommitHash);
        
        
        //conflit最后抛异常
        if (hasConflits)
        {
            throw new GitletException(ExceptionMessages.MergeConflict);
        }
        
    }
    
    
}