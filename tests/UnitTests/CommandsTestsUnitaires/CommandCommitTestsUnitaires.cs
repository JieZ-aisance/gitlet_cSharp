using Gitlet.Commands;
using Shouldly;

namespace Gitlet.Tests.UnitTests;

public class CommandCommitTestsUnitaires : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "gitlet-commit-" + Guid.NewGuid());
    private readonly GitletContexte _contexte;
    private readonly CommandCommit _commit = new();

    public CommandCommitTestsUnitaires()
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
        _commit.ExpectedArgs.ShouldBe(2);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Execute_EmptyMessage_Throws(string? message)
    {
        // Arrange
        File.WriteAllText(Path.Combine(_dir, "a.txt"), "x");

        new CommandAdd().Execute(_contexte, ["add", "a.txt"]);

        // Act & Assert
        Should.Throw<GitletException>(() => _commit.Execute(_contexte, ["commit", message!]))
            .Message.ShouldBe(ExceptionMessages.PleaseEnterCommitMessage);
    }

    [Fact]
    public void Execute_NothingStaged_Throws()
    {
        // Arrange
        var before = _contexte.Repository.GetCommitHash();

        // Act & Assert
        Should.Throw<GitletException>(() => _commit.Execute(_contexte, ["commit", "empty"]))
            .Message.ShouldBe(ExceptionMessages.NoChangesAdded);

        // 分支没有移动
        _contexte.Repository.GetCommitHash().ShouldBe(before);
    }

    [Fact]
    public void Execute_ReturnsConfirmationMessage()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_dir, "a.txt"), "x");

        new CommandAdd().Execute(_contexte, ["add", "a.txt"]);

        // Act
        var result = _commit.Execute(_contexte, ["commit", "first"]);

        // Assert
        result.ShouldBe("Commit successful with message: first");
    }

    [Fact]
    public void Execute_CreatesCommitWithMessageParentAndStagedFiles()
    {
        // Arrange
        var parentHash = _contexte.Repository.GetCommitHash();

        File.WriteAllText(Path.Combine(_dir, "a.txt"), "x");

        new CommandAdd().Execute(_contexte, ["add", "a.txt"]);

        var stagedHash = _contexte.Repository.ReadStaging().Additions["a.txt"];

        // Act
        _commit.Execute(_contexte, ["commit", "first"]);

        // Assert
        var commit = _contexte.Repository.ReadCommit();

        commit.Message.ShouldBe("first");

        commit.ParentHashes.ShouldBe([parentHash]);

        commit.Files.Count.ShouldBe(1);

        commit.Files["a.txt"].ShouldBe(stagedHash);

        // 当前分支指向新的 commit
        _contexte.Repository.GetCommitHash().ShouldNotBe(parentHash);
    }

    [Fact]
    public void Execute_ClearsStaging()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_dir, "a.txt"), "x");

        new CommandAdd().Execute(_contexte, ["add", "a.txt"]);

        // Act
        _commit.Execute(_contexte, ["commit", "first"]);

        // Assert
        var staging = _contexte.Repository.ReadStaging();

        staging.Additions.ShouldBeEmpty();

        staging.Removals.ShouldBeEmpty();
    }

    [Fact]
    public void Execute_KeepsFilesFromPreviousCommit()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_dir, "a.txt"), "a1");

        new CommandAdd().Execute(_contexte, ["add", "a.txt"]);

        _commit.Execute(_contexte, ["commit", "add a"]);

        var hashOfA = _contexte.Repository.ReadCommit().Files["a.txt"];

        File.WriteAllText(Path.Combine(_dir, "b.txt"), "b1");

        new CommandAdd().Execute(_contexte, ["add", "b.txt"]);

        // Act
        _commit.Execute(_contexte, ["commit", "add b"]);

        // Assert
        var commit = _contexte.Repository.ReadCommit();

        commit.Files.Keys.ShouldBe(["a.txt", "b.txt"]);

        commit.Files["a.txt"].ShouldBe(hashOfA);
    }

    [Fact]
    public void Execute_ModifiedFile_OverwritesHashAndLeavesParentCommitUntouched()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_dir, "a.txt"), "v1");

        new CommandAdd().Execute(_contexte, ["add", "a.txt"]);

        _commit.Execute(_contexte, ["commit", "first"]);

        var firstCommitHash = _contexte.Repository.GetCommitHash();

        var firstBlobHash = _contexte.Repository.ReadCommit().Files["a.txt"];

        File.WriteAllText(Path.Combine(_dir, "a.txt"), "v2");

        new CommandAdd().Execute(_contexte, ["add", "a.txt"]);

        // Act
        _commit.Execute(_contexte, ["commit", "second"]);

        // Assert
        _contexte.Repository.ReadCommit().Files["a.txt"].ShouldNotBe(firstBlobHash);

        // 父 commit 里的版本不变
        _contexte.Repository.ReadCommit(firstCommitHash).Files["a.txt"].ShouldBe(firstBlobHash);
    }

    [Fact]
    public void Execute_WithStagedRemoval_DropsFileFromCommit()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_dir, "a.txt"), "a1");

        new CommandAdd().Execute(_contexte, ["add", "a.txt"]);

        _commit.Execute(_contexte, ["commit", "add a"]);

        new CommandRm().Execute(_contexte, ["rm", "a.txt"]);

        // Act
        _commit.Execute(_contexte, ["commit", "remove a"]);

        // Assert
        _contexte.Repository.ReadCommit().Files.ShouldBeEmpty();

        _contexte.Repository.ReadStaging().Removals.ShouldBeEmpty();
    }

    [Fact]
    public void Execute_MovesOnlyTheCurrentBranch()
    {
        // Arrange
        new CommandBranch().Execute(_contexte, ["branch", "dev"]);

        var before = _contexte.Repository.GetCommitHash();

        File.WriteAllText(Path.Combine(_dir, "a.txt"), "x");

        new CommandAdd().Execute(_contexte, ["add", "a.txt"]);

        // Act
        _commit.Execute(_contexte, ["commit", "on main"]);

        // Assert
        File.ReadAllText(_contexte.Paths.BranchFile("dev")).ShouldBe(before);

        File.ReadAllText(_contexte.Paths.BranchFile("main")).ShouldNotBe(before);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(5)]
    public void Execute_SeveralCommits_ChainsEachOneToThePrevious(int count)
    {
        // Arrange
        var expectedParent = _contexte.Repository.GetCommitHash();

        for (var i = 1; i <= count; i++)
        {
            File.WriteAllText(Path.Combine(_dir, "a.txt"), $"v{i}");

            new CommandAdd().Execute(_contexte, ["add", "a.txt"]);

            // Act
            _commit.Execute(_contexte, ["commit", $"message {i}"]);

            // Assert
            var commit = _contexte.Repository.ReadCommit();

            commit.Message.ShouldBe($"message {i}");

            commit.ParentHashes.ShouldBe([expectedParent]);

            expectedParent = _contexte.Repository.GetCommitHash();
        }
    }
}
