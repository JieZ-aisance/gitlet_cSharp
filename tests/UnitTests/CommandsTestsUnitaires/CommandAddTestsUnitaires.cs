using Gitlet.Commands;
using Shouldly;

namespace Gitlet.Tests.UnitTests;

public class CommandAddTestsUnitaires : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "gitlet-add-" + Guid.NewGuid());
    private readonly GitletContexte _contexte;
    private readonly CommandAdd _add = new();

    public CommandAddTestsUnitaires()
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
        _add.ExpectedArgs.ShouldBe(2);
    }

    [Fact]
    public void Execute_MissingFile_Throws()
    {
        // Act & Assert
        Should.Throw<GitletException>(() => _add.Execute(_contexte, ["add", "nope.txt"]))
            .Message.ShouldBe(ExceptionMessages.FileDoesNotExist);

        // 失败时不应该写暂存区
        File.Exists(_contexte.Paths.StagingFile).ShouldBeFalse();
    }

    [Fact]
    public void Execute_ReturnsConfirmationMessage()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_dir, "a.txt"), "hello");

        // Act
        var result = _add.Execute(_contexte, ["add", "a.txt"]);

        // Assert
        result.ShouldBe("a.txt added");
    }

    [Theory]
    [InlineData("hello", "b6fc4c620b67d95f953a5c1c1230aaab5db5a1b0")]
    [InlineData("", "e69de29bb2d1d6434b8b29ae775ad8c2e48c5391")]
    public void Execute_NewFile_StoresBlobAndStagesItsHash(string content, string expectedBlobHash)
    {
        // Arrange
        File.WriteAllText(Path.Combine(_dir, "a.txt"), content);

        // Act
        _add.Execute(_contexte, ["add", "a.txt"]);

        // Assert
        _contexte.Objects.Exists("blob", expectedBlobHash).ShouldBeTrue();

        var staging = _contexte.State.ReadStaging();

        staging.Additions.Count.ShouldBe(1);

        staging.Additions["a.txt"].ShouldBe(expectedBlobHash);

        staging.Removals.ShouldBeEmpty();
    }

    [Fact]
    public void Execute_SameFileAgainWithNewContent_OverwritesStagedHash()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_dir, "a.txt"), "v1");

        _add.Execute(_contexte, ["add", "a.txt"]);

        var firstHash = _contexte.State.ReadStaging().Additions["a.txt"];

        File.WriteAllText(Path.Combine(_dir, "a.txt"), "v2");

        // Act
        _add.Execute(_contexte, ["add", "a.txt"]);

        // Assert
        var staging = _contexte.State.ReadStaging();

        staging.Additions.Count.ShouldBe(1);

        staging.Additions["a.txt"].ShouldNotBe(firstHash);
    }

    [Fact]
    public void Execute_SeveralFiles_KeepsAllOfThemInSortedOrder()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_dir, "b.txt"), "b");

        File.WriteAllText(Path.Combine(_dir, "a.txt"), "a");

        // Act
        _add.Execute(_contexte, ["add", "b.txt"]);

        _add.Execute(_contexte, ["add", "a.txt"]);

        // Assert
        _contexte.State.ReadStaging().Additions.Keys.ShouldBe(["a.txt", "b.txt"]);
    }

    [Fact]
    public void Execute_UnchangedSinceLastCommit_DoesNotStage()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_dir, "a.txt"), "v1");

        _add.Execute(_contexte, ["add", "a.txt"]);

        new CommandCommit().Execute(_contexte, ["commit", "first"]);

        // Act
        _add.Execute(_contexte, ["add", "a.txt"]);

        // Assert
        _contexte.State.ReadStaging().Additions.ShouldBeEmpty();
    }

    [Fact]
    public void Execute_ModifiedSinceLastCommit_StagesNewHash()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_dir, "a.txt"), "v1");

        _add.Execute(_contexte, ["add", "a.txt"]);

        new CommandCommit().Execute(_contexte, ["commit", "first"]);

        var committedHash = _contexte.State.ReadCommit().Files["a.txt"];

        File.WriteAllText(Path.Combine(_dir, "a.txt"), "v2");

        // Act
        _add.Execute(_contexte, ["add", "a.txt"]);

        // Assert
        var staging = _contexte.State.ReadStaging();

        staging.Additions.ContainsKey("a.txt").ShouldBeTrue();

        staging.Additions["a.txt"].ShouldNotBe(committedHash);
    }

    [Fact]
    public void Execute_StagedThenRevertedToCommittedVersion_RemovesFromAdditions()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_dir, "a.txt"), "v1");

        _add.Execute(_contexte, ["add", "a.txt"]);

        new CommandCommit().Execute(_contexte, ["commit", "first"]);

        // 改成 v2 并暂存,再改回 v1
        File.WriteAllText(Path.Combine(_dir, "a.txt"), "v2");

        _add.Execute(_contexte, ["add", "a.txt"]);

        File.WriteAllText(Path.Combine(_dir, "a.txt"), "v1");

        // Act
        _add.Execute(_contexte, ["add", "a.txt"]);

        // Assert
        _contexte.State.ReadStaging().Additions.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("v1", false)] // 内容和 commit 一样:只取消删除
    [InlineData("v2", true)]  // 内容变了:取消删除,并重新暂存
    public void Execute_FileMarkedForRemoval_CancelsTheRemoval(string newContent, bool expectedStaged)
    {
        // Arrange
        File.WriteAllText(Path.Combine(_dir, "a.txt"), "v1");

        _add.Execute(_contexte, ["add", "a.txt"]);

        new CommandCommit().Execute(_contexte, ["commit", "first"]);

        new CommandRm().Execute(_contexte, ["rm", "a.txt"]);

        File.WriteAllText(Path.Combine(_dir, "a.txt"), newContent);

        // Act
        _add.Execute(_contexte, ["add", "a.txt"]);

        // Assert
        var staging = _contexte.State.ReadStaging();

        staging.Removals.ShouldBeEmpty();

        staging.Additions.ContainsKey("a.txt").ShouldBe(expectedStaged);
    }

    [Fact]
    public void Execute_DoesNotTouchOtherStagedEntries()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_dir, "old.txt"), "x");

        _add.Execute(_contexte, ["add", "old.txt"]);

        new CommandCommit().Execute(_contexte, ["commit", "add old"]);

        new CommandRm().Execute(_contexte, ["rm", "old.txt"]);

        File.WriteAllText(Path.Combine(_dir, "a.txt"), "a");

        _add.Execute(_contexte, ["add", "a.txt"]);

        File.WriteAllText(Path.Combine(_dir, "b.txt"), "b");

        // Act
        _add.Execute(_contexte, ["add", "b.txt"]);

        // Assert
        var staging = _contexte.State.ReadStaging();

        staging.Removals.ShouldBe(["old.txt"]);

        staging.Additions.Keys.ShouldBe(["a.txt", "b.txt"]);
    }
}
