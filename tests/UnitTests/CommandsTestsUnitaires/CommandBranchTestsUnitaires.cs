using Gitlet.Commands;
using Shouldly;

namespace Gitlet.Tests.UnitTests;

public class CommandBranchTestsUnitaires : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "gitlet-branch-" + Guid.NewGuid());
    private readonly GitletContexte _contexte;
    private readonly CommandBranch _branch = new();

    public CommandBranchTestsUnitaires()
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
        _branch.ExpectedArgs.ShouldBe(2);
    }

    [Fact]
    public void Execute_ReturnsConfirmationMessage()
    {
        // Act
        var result = _branch.Execute(_contexte, ["branch", "dev"]);

        // Assert
        result.ShouldBe("dev created");
    }

    [Fact]
    public void Execute_CreatesBranchWithoutSwitching()
    {
        // Act
        _branch.Execute(_contexte, ["branch", "dev"]);

        // Assert
        File.Exists(_contexte.Paths.BranchFile("dev")).ShouldBeTrue();

        _contexte.State.CurrentBranch().ShouldBe("main");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(3)]
    public void Execute_PointsAtCurrentCommit(int commitsBefore)
    {
        // Arrange
        for (var i = 0; i < commitsBefore; i++)
        {
            File.WriteAllText(Path.Combine(_dir, "a.txt"), $"v{i}");

            new CommandAdd().Execute(_contexte, ["add", "a.txt"]);

            new CommandCommit().Execute(_contexte, ["commit", $"c{i}"]);
        }

        // Act
        _branch.Execute(_contexte, ["branch", "dev"]);

        // Assert
        File.ReadAllText(_contexte.Paths.BranchFile("dev")).ShouldBe(_contexte.State.GetCommitHash());
    }

    [Fact]
    public void Execute_DoesNotMoveWhenCurrentBranchAdvances()
    {
        // Arrange
        _branch.Execute(_contexte, ["branch", "dev"]);

        var hashAtCreation = _contexte.State.GetCommitHash();

        File.WriteAllText(Path.Combine(_dir, "a.txt"), "x");

        new CommandAdd().Execute(_contexte, ["add", "a.txt"]);

        // Act
        new CommandCommit().Execute(_contexte, ["commit", "on main"]);

        // Assert
        File.ReadAllText(_contexte.Paths.BranchFile("dev")).ShouldBe(hashAtCreation);
    }

    [Theory]
    [InlineData("main")]
    [InlineData("dev")]
    public void Execute_ExistingName_Throws(string name)
    {
        // Arrange
        _branch.Execute(_contexte, ["branch", "dev"]);

        // Act & Assert
        Should.Throw<GitletException>(() => _branch.Execute(_contexte, ["branch", name]))
            .Message.ShouldBe(ExceptionMessages.BranchAlreadyExists);
    }
}
