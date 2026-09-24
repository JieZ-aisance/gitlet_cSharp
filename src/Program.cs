using System.Text;
namespace Gitlet;

/** Driver class for Gitlet, a subset of the Git version-control system.
 *  @author TODO
 */
public static class Program
{
    public static void Main(string[] args)//注意这里
    {
        
        var repo = new Repository(Directory.GetCurrentDirectory());
        switch (args[0])
        {
            case "init":
                repo.Init();
                break;
            case "add":
                break;
            case "commit":
                break;
            default:
                Console.WriteLine("Unknown command");
                break;
            
            
        }
    }
    
}
