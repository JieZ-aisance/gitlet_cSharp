using System.Text;
using System.Globalization;

namespace Gitlet.Commands;

public sealed class CommandGlobalLog : ICommand
{
    public int ExpectedArgs => 1;
    public string Execute(GitletContexte contexte, string[] args)
    {
        var infomationToPrint = new StringBuilder();
        foreach (var commit in Directory.GetDirectories(contexte.Paths.CommitDir))
        {
            var prefixe = Path.GetFileName(commit);

            foreach (var file in Directory.GetFiles(commit))
            {
                var hashtoprint = prefixe+Path.GetFileName(file);
                var CurrentCommit = contexte.Repository.ReadCommit(hashtoprint);
                
                var date = CurrentCommit.TimeStamp.ToLocalTime()
                    .ToString("ddd MMM d HH:mm:ss yyyy zzz", CultureInfo.InvariantCulture); 
           
                var message = CurrentCommit.Message;
                
                //apprend
                infomationToPrint.Append("==============\n");
                infomationToPrint.Append("commit  ");
                infomationToPrint.Append(hashtoprint);
                infomationToPrint.Append("\n");
                infomationToPrint.Append("Date ");
                infomationToPrint.Append(date);
                infomationToPrint.Append("\n");
                infomationToPrint.Append(message);
                infomationToPrint.Append("\n");
                
            }
        }
        return infomationToPrint.ToString();
    }
}
