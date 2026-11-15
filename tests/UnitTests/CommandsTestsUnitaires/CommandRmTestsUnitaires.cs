using Gitlet.Commands;
using Shouldly;

namespace Gitlet.Tests.UnitTests;

public class CommandRmTestsUnitaires : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "gitlet-rm-" + Guid.NewGuid());
    private readonly GitletContexte _contexte;
    private readonly CommandRm _rm = new();

    public CommandRmTestsUnitaires()
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
    public void ExpectedArgs_IsTwo()
    {
        // Act & Assert
        _rm.ExpectedArgs.ShouldBe(2);
    }

    [Theory]
    [InlineData(true)]  // 工作区有这个文件,但没暂存也没跟踪
    [InlineData(false)] // 文件根本不存在
    public void Execute_NeitherStagedNorTracked_Throws(bool fileExists)
    {
        // Arrange
        if (fileExists)
        {
            File.WriteAllText(Path.Combine(_dir, "a.txt"), "x");
        }

        // Act & Assert
        Should.Throw<GitletException>(() => _rm.Execute(_contexte, ["rm", "a.txt"]))
            .Message.ShouldBe(ExceptionMessages.NoReasonToRemove);

        // 工作区的文件没被动过
        File.Exists(Path.Combine(_dir, "a.txt")).ShouldBe(fileExists);
    }

    [Fact]
    public void Execute_ReturnsConfirmationMessage()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_dir, "a.txt"), "x");

        new CommandAdd().Execute(_contexte, ["add", "a.txt"]);

        // Act
        var result = _rm.Execute(_contexte, ["rm", "a.txt"]);

        // Assert
        result.ShouldBe("a.txt removed");
    }

    [Fact]
    public void Execute_StagedButNotTracked_UnstagesAndKeepsFile()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_dir, "a.txt"), "x");

        new CommandAdd().Execute(_contexte, ["add", "a.txt"]);

        // Act
        _rm.Execute(_contexte, ["rm", "a.txt"]);

        // Assert
        var staging = _contexte.Repository.ReadStaging();

        staging.Additions.ShouldBeEmpty();

        staging.Removals.ShouldBeEmpty();

        File.Exists(Path.Combine(_dir, "a.txt")).ShouldBeTrue();
    }

    [Fact]
    public void Execute_Tracked_StagesRemovalAndDeletesFile()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_dir, "a.txt"), "x");

        new CommandAdd().Execute(_contexte, ["add", "a.txt"]);

        new CommandCommit().Execute(_contexte, ["commit", "add a"]);

        // Act
        _rm.Execute(_contexte, ["rm", "a.txt"]);

        // Assert
        _contexte.Repository.ReadStaging().Removals.ShouldBe(["a.txt"]);

        File.Exists(Path.Combine(_dir, "a.txt")).ShouldBeFalse();
    }

    [Fact]
    public void Execute_TrackedButAlreadyDeletedByUser_StillStagesRemoval()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_dir, "a.txt"), "x");

        new CommandAdd().Execute(_contexte, ["add", "a.txt"]);

        new CommandCommit().Execute(_contexte, ["commit", "add a"]);

        File.Delete(Path.Combine(_dir, "a.txt"));

        // Act
        _rm.Execute(_contexte, ["rm", "a.txt"]);

        // Assert
        _contexte.Repository.ReadStaging().Removals.ShouldBe(["a.txt"]);
    }

    [Fact]
    public void Execute_TrackedAndModifiedAndStaged_ClearsAdditionAndStagesRemoval()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_dir, "a.txt"), "v1");

        new CommandAdd().Execute(_contexte, ["add", "a.txt"]);

        new CommandCommit().Execute(_contexte, ["commit", "add a"]);

        File.WriteAllText(Path.Combine(_dir, "a.txt"), "v2");

        new CommandAdd().Execute(_contexte, ["add", "a.txt"]);

        // Act
        _rm.Execute(_contexte, ["rm", "a.txt"]);

        // Assert
        var staging = _contexte.Repository.ReadStaging();

        staging.Additions.ShouldBeEmpty();

        staging.Removals.ShouldBe(["a.txt"]);

        File.Exists(Path.Combine(_dir, "a.txt")).ShouldBeFalse();
    }

    [Fact]
    public void Execute_DoesNotTouchOtherFiles()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_dir, "a.txt"), "a");

        File.WriteAllText(Path.Combine(_dir, "b.txt"), "b");

        new CommandAdd().Execute(_contexte, ["add", "a.txt"]);

        new CommandAdd().Execute(_contexte, ["add", "b.txt"]);

        new CommandCommit().Execute(_contexte, ["commit", "add a and b"]);

        File.WriteAllText(Path.Combine(_dir, "c.txt"), "c");

        new CommandAdd().Execute(_contexte, ["add", "c.txt"]);

        // Act
        _rm.Execute(_contexte, ["rm", "a.txt"]);

        // Assert
        var staging = _contexte.Repository.ReadStaging();

        staging.Removals.ShouldBe(["a.txt"]);

        staging.Additions.Keys.ShouldBe(["c.txt"]);

        File.Exists(Path.Combine(_dir, "b.txt")).ShouldBeTrue();

        File.Exists(Path.Combine(_dir, "c.txt")).ShouldBeTrue();
    }
}
