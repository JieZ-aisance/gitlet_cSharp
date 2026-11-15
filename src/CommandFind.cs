using System.Text;
using System.Globalization;
using Microsoft.VisualBasic;

namespace Gitlet.Commands;

public sealed class CommandFind:ICommand
{
    public int ExpectedArgs => 2;
    
    public string Execute(GitletContexte contexte, string[] args)
    {
        var message = args[1];
        var infomationToPrint = new StringBuilder();
        var compteur = 0;
        foreach (var commit in Directory.GetDirectories(contexte.Paths.CommitDir))
        {
            var prefixe = Path.GetFileName(commit);


            foreach (var file in Directory.GetFiles(commit))
            {
                var hashtoprint = prefixe + Path.GetFileName(file);

                var CurrentCommit = contexte.State.ReadCommit(hashtoprint);

                // var date = CurrentCommit.TimeStamp.ToLocalTime()
                //     .ToString("ddd MMM d HH:mm:ss yyyy zzz", CultureInfo.InvariantCulture);

                if (message == CurrentCommit.Message)
                {
                    infomationToPrint.Append(hashtoprint);

                    infomationToPrint.Append("\n");

                    compteur++;
                }
            }
        }

        if (compteur == 0)
        {
            throw new GitletException(ExceptionMessages.NoCommitWithMessage);
        }
        return infomationToPrint.ToString();
    }
    
}