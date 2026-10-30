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
    

