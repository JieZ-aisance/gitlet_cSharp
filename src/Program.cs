using System.Text;
namespace Gitlet;

/** Driver class for Gitlet, a subset of the Git version-control system.
 */
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

            var repo = new Repository(Directory.GetCurrentDirectory());
            
            var mergescope = new GitletMergeScope(Directory.GetCurrentDirectory());

            switch (args[0])
            {
                case "init":
                    RequireArgs(args, 1);
                    repo.Init();
                    break;
                case "add":
                    RequireArgs(args, 2);
                    repo.GitletAdd(args[1]);
                    break;
                case "commit":
                    RequireArgs(args, 2);
                    repo.GitletCommit(args[1]);
                    break;
                case "rm":
                    RequireArgs(args, 2);
                    repo.GitletDelete(args[1]);
                    break;
                case "log":
                    RequireArgs(args, 1);
                    Console.Write(repo.GitletLog());
                    break;
                case "global-log":
                    RequireArgs(args, 1);
                    Console.Write(repo.GitletGlobalLog());
                    break;
                case "find":
                    RequireArgs(args, 2);
                    Console.Write(repo.GitletFind(args[1]));
                    break;
                case "branch":
                    RequireArgs(args, 2);
                    repo.GitletBranch(args[1]);
                    break;
                case "rm-branch":
                    RequireArgs(args, 2);
                    repo.GitletRmBranch(args[1]);
                    break;
                case "status":
                    RequireArgs(args, 1);
                    Console.Write(repo.GitletStatus());
                    break;
                case "checkout":
                    if (args.Length == 3 && args[1] == "--")
                    {
                        // checkout -- <file>
                        repo.GitletCheckoutFile(args[2]);
                    }
                    else if (args.Length == 4 && args[2] == "--")
                    {
                        // checkout <commit id> -- <file>
                        repo.GitletCheckoutCommitFile(args[1], args[3]);
                    }
                    else if (args.Length == 2)
                    {
                        // checkout <branch>
                        repo.GitletCheckoutBranch(args[1]);
                    }
                    else
                    {
                        throw new GitletException(ExceptionMessages.IncorrectOperands);
                    }
                    break;
                
                case "reset":
                    RequireArgs(args, 2);
                    repo.GitletReset(args[1]);
                    break;
                
                case "merge" :
                    RequireArgs(args, 2);
                    mergescope.GitletMerge(args[1]);
                    break;
                
                
                default:
                    throw new GitletException(ExceptionMessages.NoSuchCommand);
                
                   
            }
        }

        catch (GitletException e)
        {
            // 只打印消息,不打印堆栈
            Console.WriteLine(e.Message);

        }
    }

    // 参数个数(含命令本身)不对 → spec 的 Incorrect operands. excepted包含add,commit本身
        private static void RequireArgs(string[] args, int expected)
        {
            if (args.Length != expected)
            {
                throw new GitletException(ExceptionMessages.IncorrectOperands);
            }
        }
    }
    

