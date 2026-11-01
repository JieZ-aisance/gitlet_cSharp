using System.Text;
namespace Gitlet;

/** Driver class for Gitlet, a subset of the Git version-control system.
 *  @author TODO
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

            switch (args[0])
            {
                case "init":
                    repo.Init();
                    break;
                case "add":
                    repo.GitletAdd(args[1]);
                    break;
                case "commit":
                    repo.GitletCommit(args[1]);
                    break;
                case "rm":
                    repo.GitletDelete(args[1]);
                    break;
                case "log":
                    Console.Write(repo.GitletLog());
                    break;
                case "global-log":
                    Console.Write(repo.GitletGlobalLog());
                    break;
                case "find":
                    Console.Write(repo.GitletFind(args[1]));
                    break;
                case "branch":
                    repo.GitletBranch(args[1]);
                    break;
                case "rmbranch":
                    repo.GitletRmBranch(args[1]);
                    break;
                case "status":
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
                        // checkout <branch>,还没写
                        throw new GitletException(ExceptionMessages.IncorrectOperands);
                    }
                    else
                    {
                        throw new GitletException(ExceptionMessages.IncorrectOperands);
                    }
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

    // 参数个数(含命令本身)不对 → spec 的 Incorrect operands.
        private static void RequireArgs(string[] args, int expected)
        {
            if (args.Length != expected)
            {
                throw new GitletException(ExceptionMessages.IncorrectOperands);
            }
        }
    }
    

