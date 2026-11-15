using System.Text;
namespace Gitlet.Commands;

public sealed class CommandStatus:ICommand
{
    public int ExpectedArgs => 1;
    
    public string Execute(GitletContexte contexte, string[] args)
    {
        
        var infomationToPrint = new StringBuilder();
        
        infomationToPrint.Append("=== Branches ===\n");

        var current = contexte.Repository.CurrentBranch();
        
        var branches = Directory.GetFiles(contexte.Paths.RefHeadDirec)
            .Select(Path.GetFileName)
            .OrderBy(name => name, StringComparer.Ordinal);
        
      
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
        

        var staging = contexte.Repository.ReadStaging();

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