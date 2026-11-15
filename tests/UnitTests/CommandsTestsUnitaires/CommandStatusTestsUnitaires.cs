using Gitlet.Commands;
using Shouldly;

namespace Gitlet.Tests.UnitTests;

public class CommandStatusTestsUnitaires : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "gitlet-status-" + Guid.NewGuid());
    private readonly GitletContexte _contexte;
    private readonly CommandStatus _status = new();

    public CommandStatusTestsUnitaires()
    {
        Directory.CreateDirectory(_dir);

        _contexte = new GitletContexte(_dir);

        // 每个测试都从一个刚 init 的仓库开始
        new CommandInit().Execute(_contexte, ["init"]);
    }

    public void Dispose()
    {
        if (Directory.Exists(_dir))
        {
            Directory.Delete(_dir, recursive: true);
        }
    }

    [Fact]
    public void ExpectedArgs_IsOne()
    {
        // Act & Assert
        _status.ExpectedArgs.ShouldBe(1);
    }

    [Fact]
    public void Execute_FreshRepository_HasExactLayout()
    {
        // Act
        var result = _status.Execute(_contexte, ["status"]);

        // Assert
        result.ShouldBe(
            "=== Branches ===\n*main\n\n" +
            "=== Staged Files ===\n\n" +
            "=== Removed Files ===\n\n" +
            "=== Modifications Not Staged For Commit ===\n\n" +
            "=== Untracked Files ===\n\n");
    }

    [Fact]
    public void Execute_BranchesAreSortedAndCurrentOneIsMarked()
    {
        // Arrange
        new CommandBranch().Execute(_contexte, ["branch", "zeta"]);

        new CommandBranch().Execute(_contexte, ["branch", "alpha"]);

        // Act
        var result = _status.Execute(_contexte, ["status"]);

        // Assert
        result.ShouldStartWith("=== Branches ===\nalpha\n*main\nzeta\n\n");
    }

    [Fact]
    public void Execute_AfterCheckout_MarksTheNewCurrentBranch()
    {
        // Arrange
        new CommandBranch().Execute(_contexte, ["branch", "dev"]);

        new CommandCheckoutBranch().Execute(_contexte, ["checkout", "dev"]);

        // Act
        var result = _status.Execute(_contexte, ["status"]);

        // Assert
        result.ShouldStartWith("=== Branches ===\n*dev\nmain\n\n");
    }

    [Fact]
    public void Execute_ShowsStagedFilesInSortedOrder()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_dir, "b.txt"), "b");

        File.WriteAllText(Path.Combine(_dir, "a.txt"), "a");

        new CommandAdd().Execute(_contexte, ["add", "b.txt"]);

        new CommandAdd().Execute(_contexte, ["add", "a.txt"]);

        // Act
        var result = _status.Execute(_contexte, ["status"]);

        // Assert
        result.ShouldContain("=== Staged Files ===\na.txt\nb.txt\n\n");

        result.ShouldContain("=== Removed Files ===\n\n");
    }

    [Fact]
    public void Execute_ShowsRemovedFilesInSortedOrder()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_dir, "b.txt"), "b");

        File.WriteAllText(Path.Combine(_dir, "a.txt"), "a");

        new CommandAdd().Execute(_contexte, ["add", "b.txt"]);

        new CommandAdd().Execute(_contexte, ["add", "a.txt"]);

        new CommandCommit().Execute(_contexte, ["commit", "add a and b"]);

        new CommandRm().Execute(_contexte, ["rm", "b.txt"]);

        new CommandRm().Execute(_contexte, ["rm", "a.txt"]);

        // Act
        var result = _status.Execute(_contexte, ["status"]);

        // Assert
        result.ShouldContain("=== Staged Files ===\n\n");

        result.ShouldContain("=== Removed Files ===\na.txt\nb.txt\n\n");
    }

    [Fact]
    public void Execute_ShowsStagedAndRemovedFilesTogether()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_dir, "old.txt"), "x");

        new CommandAdd().Execute(_contexte, ["add", "old.txt"]);

        new CommandCommit().Execute(_contexte, ["commit", "add old"]);

        new CommandRm().Execute(_contexte, ["rm", "old.txt"]);

        File.WriteAllText(Path.Combine(_dir, "new.txt"), "y");

        new CommandAdd().Execute(_contexte, ["add", "new.txt"]);

        // Act
        var result = _status.Execute(_contexte, ["status"]);

        // Assert
        result.ShouldBe(
            "=== Branches ===\n*main\n\n" +
            "=== Staged Files ===\nnew.txt\n\n" +
            "=== Removed Files ===\nold.txt\n\n" +
            "=== Modifications Not Staged For Commit ===\n\n" +
            "=== Untracked Files ===\n\n");
    }

    [Fact]
    public void Execute_AfterCommit_StagingSectionsAreEmpty()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_dir, "a.txt"), "x");

        new CommandAdd().Execute(_contexte, ["add", "a.txt"]);

        new CommandCommit().Execute(_contexte, ["commit", "first"]);

        // Act
        var result = _status.Execute(_contexte, ["status"]);

        // Assert
        result.ShouldContain("=== Staged Files ===\n\n");

        result.ShouldContain("=== Removed Files ===\n\n");
    }
}
