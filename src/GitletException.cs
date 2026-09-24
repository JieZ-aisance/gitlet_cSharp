namespace Gitlet;

/** General exception indicating a Gitlet error.  For fatal errors, the
*  result of .getMessage() is the error message to be printed.
*  @author P. N. Hilfinger
    */
public sealed class GitletException : Exception
{
    public GitletException(string message) : base(message) { }
}