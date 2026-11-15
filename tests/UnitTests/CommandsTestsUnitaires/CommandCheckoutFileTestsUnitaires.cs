using Gitlet.Commands;
using Shouldly;

namespace Gitlet.Tests.UnitTests;

public class CommandCheckoutFileTestsUnitaires : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "gitlet-checkoutfile-" + Guid.NewGuid());
    private readonly GitletContexte _contexte;
    private readonly CommandCheckoutFile _checkoutFile = new();

    public CommandCheckoutFileTestsUnitaires()
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
    public void ExpectedArgs_IsThree()
    {
        // Act & Assert
        _checkoutFile.ExpectedArgs.ShouldBe(3);
    }

    [Fact]
    public void Execute_ReturnsConfirmationMessage()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_dir, "a.txt"), "committed");

        new CommandAdd().Execute(_contexte, ["add", "a.txt"]);

        new CommandCommit().Execute(_contexte, ["commit", "first"]);

        // Act
        var result = _checkoutFile.Execute(_contexte, ["checkout", "--", "a.txt"]);

        // Assert
        result.ShouldBe("a.txt has been restored to its version in HEAD.");
    }

    [Theory]
    [InlineData("committed")]
    [InlineData("你好\nline 2\n")]
    [InlineData("")]
    public void Execute_ModifiedFile_RestoresCommittedVersion(string committedContent)
    {
        // Arrange
        File.WriteAllText(Path.Combine(_dir, "a.txt"), committedContent);

        new CommandAdd().Execute(_contexte, ["add", "a.txt"]);

        new CommandCommit().Execute(_contexte, ["commit", "first"]);

        File.WriteAllText(Path.Combine(_dir, "a.txt"), "scribbled");

        // Act
        _checkoutFile.Execute(_contexte, ["checkout", "--", "a.txt"]);

        // Assert
        File.ReadAllText(Path.Combine(_dir, "a.txt")).ShouldBe(committedContent);
    }

    [Fact]
    public void Execute_DeletedFile_RecreatesIt()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_dir, "a.txt"), "committed");

        new CommandAdd().Execute(_contexte, ["add", "a.txt"]);

        new CommandCommit().Execute(_contexte, ["commit", "first"]);

        File.Delete(Path.Combine(_dir, "a.txt"));

        // Act
        _checkoutFile.Execute(_contexte, ["checkout", "--", "a.txt"]);

        // Assert
        File.ReadAllText(Path.Combine(_dir, "a.txt")).ShouldBe("committed");
    }

    [Fact]
    public void Execute_UsesTheHeadCommitNotAnOlderOne()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_dir, "a.txt"), "v1");

        new CommandAdd().Execute(_contexte, ["add", "a.txt"]);

        new CommandCommit().Execute(_contexte, ["commit", "first"]);

        File.WriteAllText(Path.Combine(_dir, "a.txt"), "v2");

        new CommandAdd().Execute(_contexte, ["add", "a.txt"]);

        new CommandCommit().Execute(_contexte, ["commit", "second"]);

        File.WriteAllText(Path.Combine(_dir, "a.txt"), "scribbled");

        // Act
        _checkoutFile.Execute(_contexte, ["checkout", "--", "a.txt"]);

        // Assert
        File.ReadAllText(Path.Combine(_dir, "a.txt")).ShouldBe("v2");
    }

    [Fact]
    public void Execute_DoesNotChangeStagingOrOtherFiles()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_dir, "a.txt"), "committed");

        new CommandAdd().Execute(_contexte, ["add", "a.txt"]);

        new CommandCommit().Execute(_contexte, ["commit", "first"]);

        File.WriteAllText(Path.Combine(_dir, "a.txt"), "scribbled");

        File.WriteAllText(Path.Combine(_dir, "b.txt"), "staged");

        new CommandAdd().Execute(_contexte, ["add", "b.txt"]);

        // Act
        _checkoutFile.Execute(_contexte, ["checkout", "--", "a.txt"]);

        // Assert
        var staging = _contexte.Repository.ReadStaging();

        staging.Additions.Keys.ShouldBe(["b.txt"]);

        staging.Removals.ShouldBeEmpty();

        File.ReadAllText(Path.Combine(_dir, "b.txt")).ShouldBe("staged");
    }

    [Theory]
    [InlineData(false)] // 文件完全不存在
    [InlineData(true)]  // 文件只是暂存了,还没进 commit
    public void Execute_NotInHeadCommit_Throws(bool staged)
    {
        // Arrange
        if (staged)
        {
            File.WriteAllText(Path.Combine(_dir, "a.txt"), "staged");

            new CommandAdd().Execute(_contexte, ["add", "a.txt"]);
        }

        // Act & Assert
        Should.Throw<GitletException>(() => _checkoutFile.Execute(_contexte, ["checkout", "--", "a.txt"]))
            .Message.ShouldBe(ExceptionMessages.FileDoesNotExist);
    }
}
