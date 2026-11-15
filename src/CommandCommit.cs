using System.Text;
using System.Text.Json;

namespace Gitlet.Commands;

public class CommandCommit : ICommand
{
    public int ExpectedArgs => 2;

    public string Execute(GitletContexte contexte, string[] args)
    {
        var message = args[1];
        //检查消息非空、暂存区非空
        if (string.IsNullOrEmpty(message) )
        {
            throw new GitletException(ExceptionMessages.PleaseEnterCommitMessage);
        }

        if (contexte.State.ReadStaging().Additions.Count == 0 && contexte.State.ReadStaging().Removals.Count == 0)
        {
            throw new GitletException(ExceptionMessages.NoChangesAdded);
        }
        
        //读暂存区
        var staging = contexte.State.ReadStaging();
        
        //读当前commit hash
        var commitParent = contexte.State.ReadCommit();
        
        var commitParentHash = contexte.State.GetCommitHash();
        
        // var commitParentFiles = commitParent.Files; 错误写法
        var commitParentFiles = new SortedDictionary<string, string>(commitParent.Files);
        
        // 暂存的添加 → 覆盖或新增
        foreach (var pair in staging.Additions)
        {
            commitParentFiles[pair.Key] = pair.Value;
        }

        // 暂存的删除 → 移除
        foreach (string name in staging.Removals)
        {
            commitParentFiles.Remove(name);
        }
        
        //new commit
        var newCommit = new Commit
        {
            Message = message,
            TimeStamp = DateTimeOffset.Now,
            ParentHashes = [commitParentHash],
            Files = commitParentFiles
        };
        
        //
        var json = JsonSerializer.Serialize(newCommit);
        
        //字符串转字节,这里是utf8编码，c#的默认编码是utf16，因此需要显示指定utf8
        byte[] bytes = Encoding.UTF8.GetBytes(json);
        
        //交给objectstore
        string commitHash = contexte.Objects.Write("commit", bytes);
        
        //5,写入 默认分支refs/heads/master
        var currentBranch = contexte.State.CurrentBranch();
        File.WriteAllText(contexte.Paths.BranchFile(currentBranch), commitHash);
        
        
        //clean staging
        staging.Additions.Clear();
        staging.Removals.Clear();
        
        string stagingJson = JsonSerializer.Serialize(staging);
        File.WriteAllText(contexte.Paths.StagingFile, stagingJson);
        
        
        return $"Commit successful with message: {message}";
    }
}