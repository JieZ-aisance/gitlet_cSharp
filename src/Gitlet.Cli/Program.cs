using Gitlet.Core;
// using Gitlet.Core.Repository;

namespace Gitlet.Cli;

public static class Program
{
    public static void Main(string[] args)
    {
        var repo = new Repository();
        //add
        if (args[0].Equals("add"))
        {
            //call add command
        }

        if (args[0].Equals("init"))
        {
            repo.init();
        }
        
        if (args[0].Equals("commit"))
        {
        
        }
        if (args[0].Equals("find"))
        {
            
        }
        if (args[0].Equals("switch"))
        {
         
        }

        Console.WriteLine("dododododoododo");
    }
    
    
}

    