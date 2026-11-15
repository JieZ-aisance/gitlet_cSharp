using System.Text;
using System.Globalization;

namespace Gitlet.Commands;

public sealed class CommandLog:ICommand
{
    public int ExpectedArgs => 1;
    
    public string Execute(GitletContexte contexte, string[] args)
    {
        var infomationToPrint = new StringBuilder();//optimisation var infomationToPrint = "";
       
        var CurrentCommit = contexte.State.ReadCommit();
       
        var CurrentHash = contexte.State.GetCommitHash();
       
        while (true)
        {
            //info to print
           
            var date = CurrentCommit.TimeStamp.ToLocalTime()
                .ToString("ddd MMM d HH:mm:ss yyyy zzz", CultureInfo.InvariantCulture); 
           
            var message = CurrentCommit.Message;
           
            //apprend
            infomationToPrint.Append("==============\n");
            infomationToPrint.Append("commit  ");
            infomationToPrint.Append(CurrentHash);
            infomationToPrint.Append("\n");
            infomationToPrint.Append("Date ");
            infomationToPrint.Append(date);
            infomationToPrint.Append("\n");
            infomationToPrint.Append(message);
            infomationToPrint.Append("\n");
           

           
            if (CurrentCommit.ParentHashes.Count == 0)
            {
                break;
            }
           
            CurrentHash = CurrentCommit.ParentHashes[0];
            CurrentCommit = contexte.State.ReadCommit(CurrentHash);
        }
       
        return  infomationToPrint.ToString();
    }
}