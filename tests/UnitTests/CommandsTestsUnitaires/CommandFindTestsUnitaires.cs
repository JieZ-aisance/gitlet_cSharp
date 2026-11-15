using Gitlet.Commands;
using Shouldly;

namespace Gitlet.Tests.UnitTests;

public class CommandFindTestsUnitaires : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "gitlet-find-" + Guid.NewGuid());
    private readonly GitletContexte _contexte;
    private readonly CommandFind _find = new();

    public CommandFindTestsUnitaires()
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
        _find.ExpectedArgs.ShouldBe(2);
    }

    [Fact]
    public void Execute_InitialCommitMessage_ReturnsInitialCommitHash()
    {
        // Arrange
        var expected = _contexte.State.GetCommitHash();

        // Act
        var result = _find.Execute(_contexte, ["find", "initial commit"]);

        // Assert
        result.ShouldBe(expected + "\n");
    }

    [Fact]
    public void Execute_ReturnsHashOfMatchingCommit()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_dir, "a.txt"), "1");

        new CommandAdd().Execute(_contexte, ["add", "a.txt"]);

        new CommandCommit().Execute(_contexte, ["commit", "target"]);

        var expected = _contexte.State.GetCommitHash();

        // 再提交一个别的,保证不是"返回最新的 commit"
        File.WriteAllText(Path.Combine(_dir, "a.txt"), "2");

        new CommandAdd().Execute(_contexte, ["add", "a.txt"]);

        new CommandCommit().Execute(_contexte, ["commit", "other"]);

        // Act
        var result = _find.Execute(_contexte, ["find", "target"]);

        // Assert
        result.ShouldBe(expected + "\n");
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    public void Execute_SameMessageOnSeveralCommits_ReturnsAllOfThem(int count)
    {
        // Arrange
        var expectedHashes = new List<string>();

        for (var i = 0; i < count; i++)
        {
            File.WriteAllText(Path.Combine(_dir, "a.txt"), $"v{i}");

            new CommandAdd().Execute(_contexte, ["add", "a.txt"]);

            new CommandCommit().Execute(_contexte, ["commit", "same"]);

            expectedHashes.Add(_contexte.State.GetCommitHash());
        }

        // Act
        var result = _find.Execute(_contexte, ["find", "same"]);

        // Assert
        var lines = result.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        lines.ShouldBe(expectedHashes, ignoreOrder: true);
    }

    [Fact]
    public void Execute_FindsCommitsOnOtherBranches()
    {
        // Arrange
        new CommandBranch().Execute(_contexte, ["branch", "dev"]);

        new CommandCheckoutBranch().Execute(_contexte, ["checkout", "dev"]);

        File.WriteAllText(Path.Combine(_dir, "a.txt"), "x");

        new CommandAdd().Execute(_contexte, ["add", "a.txt"]);

        new CommandCommit().Execute(_contexte, ["commit", "only on dev"]);

        var expected = _contexte.State.GetCommitHash();

        new CommandCheckoutBranch().Execute(_contexte, ["checkout", "main"]);

        // Act
        var result = _find.Execute(_contexte, ["find", "only on dev"]);

        // Assert
        result.ShouldBe(expected + "\n");
    }

    [Theory]
    [InlineData("nothing like this")]
    [InlineData("initial")]         // 只匹配一部分不算
    [InlineData("Initial Commit")]  // 区分大小写
    [InlineData("")]
    public void Execute_NoExactMatch_Throws(string message)
    {
        // Act & Assert
        Should.Throw<GitletException>(() => _find.Execute(_contexte, ["find", message]))
            .Message.ShouldBe(ExceptionMessages.NoCommitWithMessage);
    }
}
