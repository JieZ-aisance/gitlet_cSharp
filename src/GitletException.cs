namespace Gitlet;


public sealed class GitletException : Exception
{
    public GitletException(string message) : base(message) { }
}