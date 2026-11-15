using Gitlet.Commands;
using Shouldly;

namespace Gitlet.Tests.UnitTests;

public class CommandCheckoutCommitFileTestsUnitaires : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "gitlet-checkoutcommitfile-" + Guid.NewGuid());
    private readonly GitletContexte _contexte;
    private readonly CommandCheckoutCommitFile _checkoutCommitFile = new();

    public CommandCheckoutCommitFileTestsUnitaires()
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
    public void ExpectedArgs_IsFour()
    {
        // Act & Assert
        _checkoutCommitFile.ExpectedArgs.ShouldBe(4);
    }

    [Fact]
    public void Execute_ReturnsConfirmationMessage()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_dir, "a.txt"), "v1");

        new CommandAdd().Execute(_contexte, ["add", "a.txt"]);

        new CommandCommit().Execute(_contexte, ["commit", "first"]);

        var firstId = _contexte.State.GetCommitHash();

        // Act
        var result = _checkoutCommitFile.Execute(_contexte, ["checkout", firstId, "--", "a.txt"]);

        // Assert
        result.ShouldBe($"a.txt has been restored to the version in commit {firstId}.");
    }

    [Theory]
    [InlineData(40)] // 完整哈希
    [InlineData(8)]  // 缩写哈希
    public void Execute_RestoresVersionFromThatCommit(int idLength)
    {
        // Arrange
        File.WriteAllText(Path.Combine(_dir, "a.txt"), "v1");

        new CommandAdd().Execute(_contexte, ["add", "a.txt"]);

        new CommandCommit().Execute(_contexte, ["commit", "first"]);

        var firstId = _contexte.State.GetCommitHash()[..idLength];

        File.WriteAllText(Path.Combine(_dir, "a.txt"), "v2");

        new CommandAdd().Execute(_contexte, ["add", "a.txt"]);

        new CommandCommit().Execute(_contexte, ["commit", "second"]);

        // Act
        _checkoutCommitFile.Execute(_contexte, ["checkout", firstId, "--", "a.txt"]);

        // Assert
        File.ReadAllText(Path.Combine(_dir, "a.txt")).ShouldBe("v1");
    }

    [Fact]
    public void Execute_DeletedFile_RecreatesIt()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_dir, "a.txt"), "v1");

        new CommandAdd().Execute(_contexte, ["add", "a.txt"]);

        new CommandCommit().Execute(_contexte, ["commit", "first"]);

        var firstId = _contexte.State.GetCommitHash();

        // 在后面的 commit 里把文件删掉
        new CommandRm().Execute(_contexte, ["rm", "a.txt"]);

        new CommandCommit().Execute(_contexte, ["commit", "remove a"]);

        // Act
        _checkoutCommitFile.Execute(_contexte, ["checkout", firstId, "--", "a.txt"]);

        // Assert
        File.ReadAllText(Path.Combine(_dir, "a.txt")).ShouldBe("v1");
    }

    [Fact]
    public void Execute_DoesNotMoveBranchOrChangeStaging()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_dir, "a.txt"), "v1");

        new CommandAdd().Execute(_contexte, ["add", "a.txt"]);

        new CommandCommit().Execute(_contexte, ["commit", "first"]);

        var firstId = _contexte.State.GetCommitHash();

        File.WriteAllText(Path.Combine(_dir, "a.txt"), "v2");

        new CommandAdd().Execute(_contexte, ["add", "a.txt"]);

        new CommandCommit().Execute(_contexte, ["commit", "second"]);

        var headBefore = _contexte.State.GetCommitHash();

        // Act
        _checkoutCommitFile.Execute(_contexte, ["checkout", firstId, "--", "a.txt"]);

        // Assert
        _contexte.State.GetCommitHash().ShouldBe(headBefore);

        var staging = _contexte.State.ReadStaging();

        staging.Additions.ShouldBeEmpty();

        staging.Removals.ShouldBeEmpty();
    }

    [Fact]
    public void Execute_CommitFromAnotherBranch_RestoresItsVersion()
    {
        // Arrange
        new CommandBranch().Execute(_contexte, ["branch", "dev"]);

        new CommandCheckoutBranch().Execute(_contexte, ["checkout", "dev"]);

        File.WriteAllText(Path.Combine(_dir, "a.txt"), "dev version");

        new CommandAdd().Execute(_contexte, ["add", "a.txt"]);

        new CommandCommit().Execute(_contexte, ["commit", "on dev"]);

        var devId = _contexte.State.GetCommitHash();

        new CommandCheckoutBranch().Execute(_contexte, ["checkout", "main"]);

        // Act
        _checkoutCommitFile.Execute(_contexte, ["checkout", devId, "--", "a.txt"]);

        // Assert
        File.ReadAllText(Path.Combine(_dir, "a.txt")).ShouldBe("dev version");

        _contexte.State.CurrentBranch().ShouldBe("main");
    }

    [Theory]
    [InlineData("ffffffff")]
    [InlineData("zzzz")]
    public void Execute_UnknownCommit_Throws(string commitId)
    {
        // Arrange
        File.WriteAllText(Path.Combine(_dir, "a.txt"), "v1");

        new CommandAdd().Execute(_contexte, ["add", "a.txt"]);

        new CommandCommit().Execute(_contexte, ["commit", "first"]);

        // 保证这个前缀真的不匹配任何 commit
        _contexte.State.GetCommitHash().ShouldNotStartWith(commitId);

        _contexte.State.ReadCommit().ParentHashes[0].ShouldNotStartWith(commitId);

        // Act & Assert
        Should.Throw<GitletException>(() => _checkoutCommitFile.Execute(_contexte, ["checkout", commitId, "--", "a.txt"]))
            .Message.ShouldBe(ExceptionMessages.CommitDoesNotExist);
    }

    [Fact]
    public void Execute_FileNotInThatCommit_Throws()
    {
        // Arrange
        var initialId = _contexte.State.GetCommitHash();

        File.WriteAllText(Path.Combine(_dir, "a.txt"), "v1");

        new CommandAdd().Execute(_contexte, ["add", "a.txt"]);

        new CommandCommit().Execute(_contexte, ["commit", "first"]);

        // Act & Assert
        Should.Throw<GitletException>(() => _checkoutCommitFile.Execute(_contexte, ["checkout", initialId, "--", "a.txt"]))
            .Message.ShouldBe(ExceptionMessages.FileDoesNotExist);

        // 工作区的文件没被动过
        File.ReadAllText(Path.Combine(_dir, "a.txt")).ShouldBe("v1");
    }
}
