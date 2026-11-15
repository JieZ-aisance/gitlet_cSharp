using System.Text.Json;

namespace Gitlet.Commands;

// add <file name>
// 一次只能 add 一个文件
public sealed class CommandAdd : ICommand
{
    public int ExpectedArgs => 2;

    public string Execute(GitletContexte contexte, string[] args)
    {
        //Gitlet 不处理子目录,所有文件都平铺在工作目录里
        var fileName = args[1];
        var fullPath = Path.Combine(contexte.WorkingDirectory, fileName);

        if (!File.Exists(fullPath))
        {
            throw new GitletException(ExceptionMessages.FileDoesNotExist);
        }

        // 文件内容存成 blob,得到哈希
        var newHash = contexte.Objects.Write("blob", File.ReadAllBytes(fullPath));

        var commitCurrFiles = contexte.Repository.ReadCommit().Files;
        var staging = contexte.Repository.ReadStaging();

        // 避免同一个文件同时出现在添加和删除里
        staging.Removals.Remove(fileName);

        if (commitCurrFiles.TryGetValue(fileName, out string oldHash) && oldHash == newHash)
        {
            // 和当前 commit 里的版本一样:没有改动,不需要暂存
            staging.Additions.Remove(fileName);
        }
        else
        {
            // 新文件,或者内容变了:加入或覆盖
            staging.Additions[fileName] = newHash;
        }

        File.WriteAllText(contexte.Paths.StagingFile, JsonSerializer.Serialize(staging));
        
        return $"{fileName} added";
    }
}