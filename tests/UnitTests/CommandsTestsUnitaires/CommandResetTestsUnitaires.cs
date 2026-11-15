using Gitlet.Commands;
using Shouldly;

namespace Gitlet.Tests.UnitTests;

public class CommandResetTestsUnitaires : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "gitlet-reset-" + Guid.NewGuid());
    private readonly GitletContexte _contexte;
    private readonly CommandReset _reset = new();

    public CommandResetTestsUnitaires()
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
        _reset.ExpectedArgs.ShouldBe(2);
    }

    [Fact]
    public void Execute_ReturnsEmptyString()
    {
        // Arrange
        var initialId = _contexte.Repository.GetCommitHash();

        // Act
        var result = _reset.Execute(_contexte, ["reset", initialId]);

        // Assert
        result.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(40)] // 完整哈希
    [InlineData(8)]  // 缩写哈希
    public void Execute_MovesBranchAndWorkingDirectoryBackToThatCommit(int idLength)
    {
        // Arrange
        File.WriteAllText(Path.Combine(_dir, "a.txt"), "v1");

        new CommandAdd().Execute(_contexte, ["add", "a.txt"]);

        new CommandCommit().Execute(_contexte, ["commit", "first"]);

        var firstHash = _contexte.Repository.GetCommitHash();

        File.WriteAllText(Path.Combine(_dir, "a.txt"), "v2");

        new CommandAdd().Execute(_contexte, ["add", "a.txt"]);

        new CommandCommit().Execute(_contexte, ["commit", "second"]);

        File.WriteAllText(Path.Combine(_dir, "b.txt"), "b");

        new CommandAdd().Execute(_contexte, ["add", "b.txt"]);

        new CommandCommit().Execute(_contexte, ["commit", "third"]);

        // Act
        _reset.Execute(_contexte, ["reset", firstHash[..idLength]]);

        // Assert
        File.ReadAllText(Path.Combine(_dir, "a.txt")).ShouldBe("v1");

        File.Exists(Path.Combine(_dir, "b.txt")).ShouldBeFalse();

        // 分支文件里写的是完整哈希
        File.ReadAllText(_contexte.Paths.BranchFile("main")).ShouldBe(firstHash);

        _contexte.Repository.ReadCommit().Message.ShouldBe("first");
    }

    [Fact]
    public void Execute_ForwardToALaterCommit_RestoresItsFiles()
    {
        // Arrange
        var initialHash = _contexte.Repository.GetCommitHash();

        File.WriteAllText(Path.Combine(_dir, "a.txt"), "v1");

        new CommandAdd().Execute(_contexte, ["add", "a.txt"]);

        new CommandCommit().Execute(_contexte, ["commit", "first"]);

        var firstHash = _contexte.Repository.GetCommitHash();

        _reset.Execute(_contexte, ["reset", initialHash]);

        File.Exists(Path.Combine(_dir, "a.txt")).ShouldBeFalse();

        // Act
        _reset.Execute(_contexte, ["reset", firstHash]);

        // Assert
        File.ReadAllText(Path.Combine(_dir, "a.txt")).ShouldBe("v1");

        _contexte.Repository.GetCommitHash().ShouldBe(firstHash);
    }

    [Fact]
    public void Execute_MovesOnlyTheCurrentBranch()
    {
        // Arrange
        var initialHash = _contexte.Repository.GetCommitHash();

        File.WriteAllText(Path.Combine(_dir, "a.txt"), "v1");

        new CommandAdd().Execute(_contexte, ["add", "a.txt"]);

        new CommandCommit().Execute(_contexte, ["commit", "first"]);

        var firstHash = _contexte.Repository.GetCommitHash();

        new CommandBranch().Execute(_contexte, ["branch", "dev"]);

        // Act
        _reset.Execute(_contexte, ["reset", initialHash]);

        // Assert
        _contexte.Repository.CurrentBranch().ShouldBe("main");

        File.ReadAllText(_contexte.Paths.BranchFile("main")).ShouldBe(initialHash);

        File.ReadAllText(_contexte.Paths.BranchFile("dev")).ShouldBe(firstHash);
    }

    [Fact]
    public void Execute_ClearsStaging()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_dir, "a.txt"), "v1");

        File.WriteAllText(Path.Combine(_dir, "old.txt"), "old");

        new CommandAdd().Execute(_contexte, ["add", "a.txt"]);

        new CommandAdd().Execute(_contexte, ["add", "old.txt"]);

        new CommandCommit().Execute(_contexte, ["commit", "first"]);

        var firstHash = _contexte.Repository.GetCommitHash();

        // 暂存一个添加和一个删除
        File.WriteAllText(Path.Combine(_dir, "a.txt"), "v2");

        new CommandAdd().Execute(_contexte, ["add", "a.txt"]);

        new CommandRm().Execute(_contexte, ["rm", "old.txt"]);

        // Act
        _reset.Execute(_contexte, ["reset", firstHash]);

        // Assert
        var staging = _contexte.Repository.ReadStaging();

        staging.Additions.ShouldBeEmpty();

        staging.Removals.ShouldBeEmpty();

        File.ReadAllText(Path.Combine(_dir, "a.txt")).ShouldBe("v1");

        File.ReadAllText(Path.Combine(_dir, "old.txt")).ShouldBe("old");
    }

    [Fact]
    public void Execute_KeepsLaterCommitsInTheObjectStore()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_dir, "a.txt"), "v1");

        new CommandAdd().Execute(_contexte, ["add", "a.txt"]);

        new CommandCommit().Execute(_contexte, ["commit", "first"]);

        var firstHash = _contexte.Repository.GetCommitHash();

        File.WriteAllText(Path.Combine(_dir, "a.txt"), "v2");

        new CommandAdd().Execute(_contexte, ["add", "a.txt"]);

        new CommandCommit().Execute(_contexte, ["commit", "second"]);

        var secondHash = _contexte.Repository.GetCommitHash();

        // Act
        _reset.Execute(_contexte, ["reset", firstHash]);

        // Assert
        _contexte.Objects.Exists("commit", secondHash).ShouldBeTrue();

        _contexte.Repository.ReadCommit(secondHash).Message.ShouldBe("second");
    }

    [Theory]
    [InlineData("ffffffff")]
    [InlineData("zzzz")]
    public void Execute_UnknownCommit_Throws(string commitId)
    {
        // Arrange
        var before = _contexte.Repository.GetCommitHash();

        // 保证这个前缀真的不匹配任何 commit
        before.ShouldNotStartWith(commitId);

        // Act & Assert
        Should.Throw<GitletException>(() => _reset.Execute(_contexte, ["reset", commitId]))
            .Message.ShouldBe(ExceptionMessages.CommitDoesNotExist);

        _contexte.Repository.GetCommitHash().ShouldBe(before);
    }

    [Fact]
    public void Execute_UntrackedFileWouldBeOverwritten_Throws()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_dir, "a.txt"), "v1");

        new CommandAdd().Execute(_contexte, ["add", "a.txt"]);

        new CommandCommit().Execute(_contexte, ["commit", "first"]);

        var firstHash = _contexte.Repository.GetCommitHash();

        new CommandRm().Execute(_contexte, ["rm", "a.txt"]);

        new CommandCommit().Execute(_contexte, ["commit", "remove a"]);

        var headBefore = _contexte.Repository.GetCommitHash();

        File.WriteAllText(Path.Combine(_dir, "a.txt"), "untracked now");

        // Act & Assert
        Should.Throw<GitletException>(() => _reset.Execute(_contexte, ["reset", firstHash]))
            .Message.ShouldBe(ExceptionMessages.UntrackedFileInTheWay);

        // 文件没被覆盖,分支也没移动
        File.ReadAllText(Path.Combine(_dir, "a.txt")).ShouldBe("untracked now");

        _contexte.Repository.GetCommitHash().ShouldBe(headBefore);
    }
}
