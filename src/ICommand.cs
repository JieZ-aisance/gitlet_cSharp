namespace Gitlet.Commands;

public interface ICommand
{
    public int ExpectedArgs { get; }
    
    public string Execute(GitletContexte contexte, string[] args);
    
}