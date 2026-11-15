using Gitlet.Commands;
namespace Gitlet;

public static class Program
{
    public static void Main(string[] args)
    {
        try
        {
            if (args.Length == 0)
            {
                throw new GitletException(ExceptionMessages.PleaseEnterCommand);
            }

            // no init 
            if (args[0] != "init" &&
                !Directory.Exists(Path.Combine(Directory.GetCurrentDirectory(), ".gitlet")))
            {
                throw new GitletException(ExceptionMessages.NotInitialized);
            }

            var contexte = new GitletContexte(Directory.GetCurrentDirectory());

            switch (args[0])
            {
                case "init":
                    RequireArgs(args, 1);
                    new CommandInit().Execute(contexte, args);
                    break;
                case "add":
                    RequireArgs(args, 2);
                    new CommandAdd().Execute(contexte, args);
                    break;
                case "commit":
                    RequireArgs(args, 2);
                    new CommandCommit().Execute(contexte, args);
                    break;
                case "rm":
                    RequireArgs(args, 2);
                    new CommandRm().Execute(contexte, args);
                    break;
                case "log":
                    RequireArgs(args, 1);
                    // new CommandLog().Execute(contexte, args);
                    Console.Write(new CommandLog().Execute(contexte, args));
                    break;
                case "global-log":
                    RequireArgs(args, 1);
                    // new CommandGlobalLog().Execute(contexte, args);
                    Console.Write(new CommandGlobalLog().Execute(contexte, args));
                    break;
                case "find":
                    RequireArgs(args, 2);
                    Console.Write(new CommandFind().Execute(contexte, args));
                    break;
                case "branch":
                    RequireArgs(args, 2);
                    new CommandBranch().Execute(contexte, args);
                    break;
                case "rm-branch":
                    RequireArgs(args, 2);
                    new CommnadRmBranch().Execute(contexte, args);
                    break;
                case "status":
                    RequireArgs(args, 1);
                    Console.Write(new CommandStatus().Execute(contexte, args));
                    break;
                case "checkout":
                    if (args.Length == 3 && args[1] == "--")
                    {
                        // checkout -- <file>
                        new CommandCheckoutFile().Execute(contexte, args);
                    }
                    else if (args.Length == 4 && args[2] == "--")
                    {
                        // checkout <commit id> -- <file>
                        new CommandCheckoutCommitFile().Execute(contexte, args);
                    }
                    else if (args.Length == 2)
                    {
                        // checkout <branch>
                        new CommandCheckoutBranch().Execute(contexte, args);
                    }
                    else
                    {
                        throw new GitletException(ExceptionMessages.IncorrectOperands);
                    }
                    break;
                
                case "reset":
                    RequireArgs(args, 2);
                    new CommandReset().Execute(contexte, args);
                    break;
                
                case "merge" :
                    RequireArgs(args, 2);
                    new CommandMerge().Execute(contexte, args);
                    break;
                
                default:
                    throw new GitletException(ExceptionMessages.NoSuchCommand);
                
            }
        }

        catch (GitletException e)
        {
            Console.WriteLine(e.Message);
        }
    }
    
    private static void RequireArgs(string[] args, int expected)
    {
        if (args.Length != expected)
        {
            throw new GitletException(ExceptionMessages.IncorrectOperands);
        }
    }
}
    

