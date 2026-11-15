using Gitlet.Commands;
using Shouldly;

namespace Gitlet.Tests.UnitTests;

public class CommandCheckoutBranchTestsUnitaires : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "gitlet-checkoutbranch-" + Guid.NewGuid());
    private readonly GitletContexte _contexte;
    private readonly CommandCheckoutBranch _checkoutBranch = new();

    public CommandCheckoutBranchTestsUnitaires()
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
        _checkoutBranch.ExpectedArgs.ShouldBe(2);
    }

    [Fact]
    public void Execute_ReturnsConfirmationMessage()
    {
        // Arrange
        new CommandBranch().Execute(_contexte, ["branch", "dev"]);

        // Act
        var result = _checkoutBranch.Execute(_contexte, ["checkout", "dev"]);

        // Assert
        result.ShouldBe("checkout to branch dev");
    }

    [Fact]
    public void Execute_Unknown_Throws()
    {
        // Act & Assert
        Should.Throw<GitletException>(() => _checkoutBranch.Execute(_contexte, ["checkout", "ghost"]))
            .Message.ShouldBe(ExceptionMessages.BranchNoExists);

        _contexte.Repository.CurrentBranch().ShouldBe("main");
    }

    [Fact]
    public void Execute_CurrentBranch_Throws()
    {
        // Act & Assert
        Should.Throw<GitletException>(() => _checkoutBranch.Execute(_contexte, ["checkout", "main"]))
            .Message.ShouldBe(ExceptionMessages.ALreadyOnTheBranch);
    }

    [Fact]
    public void Execute_SwitchesCurrentBranch()
    {
        // Arrange
        new CommandBranch().Execute(_contexte, ["branch", "dev"]);

        // Act
        _checkoutBranch.Execute(_contexte, ["checkout", "dev"]);

        // Assert
        File.ReadAllText(_contexte.Paths.HeadFile).ShouldBe("ref: refs/heads/dev");

        _contexte.Repository.CurrentBranch().ShouldBe("dev");
    }

    [Fact]
    public void Execute_SwapsWorkingDirectoryFiles()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_dir, "shared.txt"), "main version");

        new CommandAdd().Execute(_contexte, ["add", "shared.txt"]);

        new CommandCommit().Execute(_contexte, ["commit", "on main"]);

        new CommandBranch().Execute(_contexte, ["branch", "dev"]);

        _checkoutBranch.Execute(_contexte, ["checkout", "dev"]);

        // dev 上改 shared.txt,并新增 dev-only.txt
        File.WriteAllText(Path.Combine(_dir, "shared.txt"), "dev version");

        File.WriteAllText(Path.Combine(_dir, "dev-only.txt"), "d");

        new CommandAdd().Execute(_contexte, ["add", "shared.txt"]);

        new CommandAdd().Execute(_contexte, ["add", "dev-only.txt"]);

        new CommandCommit().Execute(_contexte, ["commit", "on dev"]);

        // Act
        _checkoutBranch.Execute(_contexte, ["checkout", "main"]);

        // Assert
        File.ReadAllText(Path.Combine(_dir, "shared.txt")).ShouldBe("main version");

        File.Exists(Path.Combine(_dir, "dev-only.txt")).ShouldBeFalse();

        // Act
        _checkoutBranch.Execute(_contexte, ["checkout", "dev"]);

        // Assert
        File.ReadAllText(Path.Combine(_dir, "shared.txt")).ShouldBe("dev version");

        File.ReadAllText(Path.Combine(_dir, "dev-only.txt")).ShouldBe("d");
    }

    [Fact]
    public void Execute_DoesNotMoveAnyBranch()
    {
        // Arrange
        new CommandBranch().Execute(_contexte, ["branch", "dev"]);

        File.WriteAllText(Path.Combine(_dir, "a.txt"), "x");

        new CommandAdd().Execute(_contexte, ["add", "a.txt"]);

        new CommandCommit().Execute(_contexte, ["commit", "on main"]);

        var mainHash = File.ReadAllText(_contexte.Paths.BranchFile("main"));

        var devHash = File.ReadAllText(_contexte.Paths.BranchFile("dev"));

        // Act
        _checkoutBranch.Execute(_contexte, ["checkout", "dev"]);

        // Assert
        File.ReadAllText(_contexte.Paths.BranchFile("main")).ShouldBe(mainHash);

        File.ReadAllText(_contexte.Paths.BranchFile("dev")).ShouldBe(devHash);

        _contexte.Repository.GetCommitHash().ShouldBe(devHash);
    }

    [Fact]
    public void Execute_ClearsStaging()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_dir, "old.txt"), "x");

        new CommandAdd().Execute(_contexte, ["add", "old.txt"]);

        new CommandCommit().Execute(_contexte, ["commit", "add old"]);

        new CommandBranch().Execute(_contexte, ["branch", "dev"]);

        // 暂存一个添加和一个删除
        File.WriteAllText(Path.Combine(_dir, "a.txt"), "x");

        new CommandAdd().Execute(_contexte, ["add", "a.txt"]);

        new CommandRm().Execute(_contexte, ["rm", "old.txt"]);

        // Act
        _checkoutBranch.Execute(_contexte, ["checkout", "dev"]);

        // Assert
        var staging = _contexte.Repository.ReadStaging();

        staging.Additions.ShouldBeEmpty();

        staging.Removals.ShouldBeEmpty();
    }

    [Fact]
    public void Execute_UntrackedFileNotInTargetBranch_IsLeftAlone()
    {
        // Arrange
        new CommandBranch().Execute(_contexte, ["branch", "dev"]);

        File.WriteAllText(Path.Combine(_dir, "notes.txt"), "untracked");

        // Act
        _checkoutBranch.Execute(_contexte, ["checkout", "dev"]);

        // Assert
        File.ReadAllText(Path.Combine(_dir, "notes.txt")).ShouldBe("untracked");
    }

    [Theory]
    [InlineData("untracked on main")]
    [InlineData("tracked on dev")] // 内容一样也不允许覆盖
    [InlineData("")]
    public void Execute_UntrackedFileWouldBeOverwritten_Throws(string untrackedContent)
    {
        // Arrange
        new CommandBranch().Execute(_contexte, ["branch", "dev"]);

        _checkoutBranch.Execute(_contexte, ["checkout", "dev"]);

        File.WriteAllText(Path.Combine(_dir, "a.txt"), "tracked on dev");

        new CommandAdd().Execute(_contexte, ["add", "a.txt"]);

        new CommandCommit().Execute(_contexte, ["commit", "dev file"]);

        _checkoutBranch.Execute(_contexte, ["checkout", "main"]);

        File.WriteAllText(Path.Combine(_dir, "a.txt"), untrackedContent);

        // Act & Assert
        Should.Throw<GitletException>(() => _checkoutBranch.Execute(_contexte, ["checkout", "dev"]))
            .Message.ShouldBe(ExceptionMessages.UntrackedFileInTheWay);

        // 文件没被覆盖,分支也没切换
        File.ReadAllText(Path.Combine(_dir, "a.txt")).ShouldBe(untrackedContent);

        _contexte.Repository.CurrentBranch().ShouldBe("main");
    }
}
