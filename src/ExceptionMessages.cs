namespace Gitlet;

public static class ExceptionMessages
{
    public const string FileDoesNotExist = "File does not exist.";
    public const string BranchAlreadyExists = "Branch already exists.";
    public const string BranchNoExists = "Branch does not exist.";
    public const string NoReasonToRemove = "No reason to remove the file.";
    public const string NoCommitWithMessage = "Found no commit with that message.";
    public const string PleaseEnterCommitMessage = "Please enter a commit message.";
    public const string NoChangesAdded = "No changes added to the commit.";
    public const string AlreadyInitialized = "A Gitlet version-control system already exists in the current directory.";
    public const string NotInitialized = "Not in an initialized Gitlet directory.";
    public const string PleaseEnterCommand = "Please enter a command.";
    public const string NoSuchCommand = "No command with that name exists.";
    public const string IncorrectOperands = "Incorrect operands.";
}