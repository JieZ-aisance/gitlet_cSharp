using System.Text.Json;

namespace Gitlet.Commands;

public sealed class CommandReset : ICommand
{
    public int ExpectedArgs => 2;
    

    public string Execute(GitletContexte contexte, string[] args)
    {
        var commitId = args[1];
        
        var commitList = new List<string>();
        
        //遍历这个目录，存在list里
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
            if (commitHash.StartsWith(commitId)) //commitId is a part of the commitHash
            {
                //
                var CurrentCommit = contexte.State.ReadCommit();
                
                var target = contexte.State.ReadCommit(commitHash);
                
                var files = target.Files;
                
                //存在未跟踪的文件
                foreach (var  file in files.Keys)
                {
                    var path = Path.Combine(contexte.WorkingDirectory, file);
                    if (File.Exists(path) && !CurrentCommit.Files.ContainsKey(file))
                    {
                        throw new GitletException(ExceptionMessages.UntrackedFileInTheWay);
                    }
                }
                
                //写指定commit中的文件
                foreach (var pair in files)
                {
                    var path = Path.Combine(contexte.WorkingDirectory, pair.Key);
                    var blobHash = pair.Value;

                    byte[] content = contexte.Objects.Read("blob", blobHash);
                    File.WriteAllBytes(Path.Combine(contexte.WorkingDirectory, pair.Key), content);
                }
                
                // 新增:当前跟踪、目标没有的文件 → 删除
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

                //同时把当前分支的头移动到那个提交节点。
                File.WriteAllText(contexte.Paths.BranchFile(contexte.State.CurrentBranch()), commitHash);
                
                //清空暂存区
                var staging = contexte.State.ReadStaging();
                staging.Additions.Clear();
                staging.Removals.Clear();
                File.WriteAllText(contexte.Paths.StagingFile, JsonSerializer.Serialize(staging));
                return "";
            }
        }

        throw new GitletException(ExceptionMessages.CommitDoesNotExist);
        
    }

}