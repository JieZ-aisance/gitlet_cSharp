using Gitlet.Commands;
using Shouldly;

namespace Gitlet.Tests.UnitTests;

public class CommandRmBranchTestsUnitaires : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "gitlet-rmbranch-" + Guid.NewGuid());
    private readonly GitletContexte _contexte;
    private readonly CommnadRmBranch _rmBranch = new();

    public CommandRmBranchTestsUnitaires()
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
        _rmBranch.ExpectedArgs.ShouldBe(2);
    }

    [Fact]
    public void Execute_RemovesTheBranch()
    {
        // Arrange
        new CommandBranch().Execute(_contexte, ["branch", "dev"]);

        // Act
        _rmBranch.Execute(_contexte, ["rm-branch", "dev"]);

        // Assert
        File.Exists(_contexte.Paths.BranchFile("dev")).ShouldBeFalse();

        // 当前分支不受影响
        File.Exists(_contexte.Paths.BranchFile("main")).ShouldBeTrue();

        _contexte.Repository.CurrentBranch().ShouldBe("main");
    }

    [Fact]
    public void Execute_KeepsTheCommitsOfTheBranch()
    {
        // Arrange
        new CommandBranch().Execute(_contexte, ["branch", "dev"]);

        new CommandCheckoutBranch().Execute(_contexte, ["checkout", "dev"]);

        File.WriteAllText(Path.Combine(_dir, "a.txt"), "x");

        new CommandAdd().Execute(_contexte, ["add", "a.txt"]);

        new CommandCommit().Execute(_contexte, ["commit", "only on dev"]);

        var devHash = _contexte.Repository.GetCommitHash();

        new CommandCheckoutBranch().Execute(_contexte, ["checkout", "main"]);

        // Act
        _rmBranch.Execute(_contexte, ["rm-branch", "dev"]);

        // Assert
        _contexte.Objects.Exists("commit", devHash).ShouldBeTrue();

        _contexte.Repository.ReadCommit(devHash).Message.ShouldBe("only on dev");
    }

    [Fact]
    public void Execute_DeletedBranchNameCanBeCreatedAgain()
    {
        // Arrange
        new CommandBranch().Execute(_contexte, ["branch", "dev"]);

        _rmBranch.Execute(_contexte, ["rm-branch", "dev"]);

        // Act
        new CommandBranch().Execute(_contexte, ["branch", "dev"]);

        // Assert
        File.Exists(_contexte.Paths.BranchFile("dev")).ShouldBeTrue();
    }

    [Fact]
    public void Execute_Unknown_Throws()
    {
        // Act & Assert
        Should.Throw<GitletException>(() => _rmBranch.Execute(_contexte, ["rm-branch", "ghost"]))
            .Message.ShouldBe(ExceptionMessages.BranchNoExists);
    }

    [Theory]
    [InlineData("main")]
    [InlineData("dev")]
    public void Execute_CurrentBranch_Throws(string currentBranch)
    {
        // Arrange
        new CommandBranch().Execute(_contexte, ["branch", "dev"]);

        if (currentBranch != "main")
        {
            new CommandCheckoutBranch().Execute(_contexte, ["checkout", currentBranch]);
        }

        // Act & Assert
        Should.Throw<GitletException>(() => _rmBranch.Execute(_contexte, ["rm-branch", currentBranch]))
            .Message.ShouldBe(ExceptionMessages.DeleteCurrentBranchForbiden);

        File.Exists(_contexte.Paths.BranchFile(currentBranch)).ShouldBeTrue();
    }
}
