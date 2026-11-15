using System.Text;
namespace Gitlet.Commands;

public sealed class CommandStatus:ICommand
{
    public int ExpectedArgs => 1;
    
    public string Execute(GitletContexte contexte, string[] args)
    {
        var infomationToPrint = new StringBuilder();
        infomationToPrint.Append("=== Branches ===\n");

        var current = contexte.State.CurrentBranch();
        var branches = Directory.GetFiles(contexte.Paths.RefHeadDirec)
            .Select(Path.GetFileName)
            .OrderBy(name => name, StringComparer.Ordinal);//按名字排序
        
        //复习linq
        //.Where(x => 条件)          // 筛选,类似 SQL 的 WHERE
        // .Select(x => 变换)         // 映射,每个元素变成另一个
        // .OrderBy(x => 排序依据)    // 排序
        // .First()                   // 取第一个
        // .Count()                   // 数量
        // .ToList()                  // 变成 List

        foreach (var name in branches)
        {
            if (name == current)
            {
                infomationToPrint.Append($"*{name}\n");
            }
            else
            {
                infomationToPrint.Append($"{name}\n");
            }
        }
        infomationToPrint.Append("\n");
        
        //staging
        var staging = contexte.State.ReadStaging();

        infomationToPrint.Append("=== Staged Files ===\n");
        foreach (var name in staging.Additions.Keys)
        {
            infomationToPrint.Append($"{name}\n");
        }
        infomationToPrint.Append("\n");

        infomationToPrint.Append("=== Removed Files ===\n");
        foreach (var name in staging.Removals)
        {
            infomationToPrint.Append($"{name}\n");
        }
        infomationToPrint.Append("\n");
        
        //TO DO
        infomationToPrint.Append("=== Modifications Not Staged For Commit ===");
        infomationToPrint.Append("\n");
        infomationToPrint.Append("\n");
        
        //TO DO
        infomationToPrint.Append("=== Untracked Files ===");
        infomationToPrint.Append("\n");
        infomationToPrint.Append("\n");
        
        return infomationToPrint.ToString();
        
    }
    
}