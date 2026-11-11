namespace Gitlet;

public static class ExceptionMessages
{
    public const string FileDoesNotExist = "File does not exist.";
    public const string UntrackedFileInTheWay = "There is an untracked file in the way; delete it, or add and commit it first.";
    public const string CommitDoesNotExist = "Commit does not exist.";
    public const string BranchAlreadyExists = "A branch with that name already exists.";
    public const string BranchNoExists = "A branch with that name does not exist.";
    public const string ALreadyOnTheBranch = "No need to checkout the current branch.";
    public const string NoReasonToRemove = "No reason to remove the file.";
    public const string NoCommitWithMessage = "Found no commit with that message.";
    public const string PleaseEnterCommitMessage = "Please enter a commit message.";
    public const string NoChangesAdded = "No changes added to the commit.";
    public const string AlreadyInitialized = "A Gitlet version-control system already exists in the current directory.";
    public const string NotInitialized = "Not in an initialized Gitlet directory.";
    public const string PleaseEnterCommand = "Please enter a command.";
    public const string NoSuchCommand = "No command with that name exists.";
    public const string IncorrectOperands = "Incorrect operands.";
    public const string DeleteCurrentBranchForbiden = "Cannot remove the current branch.";
    
    //for merge
    public const string UncommittedChanges = "You have uncommitted changes.";
    public const string CannotMergeWithItself = "Cannot merge a branch with itself.";//
    public const string GivenIsAncestor = "Given branch is an ancestor of the current branch.";//
    public const string FastForwarded = "Current branch fast-forwarded.";
    public const string MergeConflict = "Encountered a merge conflict.";
}