using Shouldly;

namespace Gitlet.Tests.IntegrationTests;

public class CommandCheckoutBranchTestsIntegration : IDisposable
{

    private readonly string _dir = Path.Combine(Path.GetTempPath(), "gitlet-checkoutbranch-cli-" + Guid.NewGuid());

    //constructor
    public CommandCheckoutBranchTestsIntegration()
    {
        Directory.CreateDirectory(_dir);
    }

    //helpers 清理每次测试后产生的文件
    public void Dispose()
    {
        if (Directory.Exists(_dir))
        {
            Directory.Delete(_dir, recursive: true);
        }
    }

    //还没 init 就 checkout
    [Fact]
    public void CheckoutBranch_BeforeInit_IsRejected()
    {
        // Act
        var result = GitletCli.Run(_dir, "checkout", "dev");

        // Assert
        result.Output.ShouldBe(ExceptionMessages.NotInitialized + Environment.NewLine);

        Directory.Exists(Path.Combine(_dir, ".gitlet")).ShouldBeFalse();
    }

    //前置检查:分支不存在 / 已经在这个分支上
    [Theory]
    [InlineData("ghost", ExceptionMessages.BranchNoExists)]
    [InlineData("main", ExceptionMessages.ALreadyOnTheBranch)]
    public void CheckoutBranch_FailedPrecondition_PrintsOnlyTheMessage(string branch, string expectedMessage)
    {
        // Arrange
        GitletCli.Run(_dir, "init");

        File.WriteAllText(Path.Combine(_dir, "a.txt"), "a");
        GitletCli.Run(_dir, "add", "a.txt");
        GitletCli.Run(_dir, "commit", "base");

        GitletCli.Run(_dir, "branch", "dev");

        var logBefore = GitletCli.Run(_dir, "log").Output;

        // Act
        var result = GitletCli.Run(_dir, "checkout", branch);

        // Assert
        result.Output.ShouldBe(expectedMessage + Environment.NewLine);
        result.Error.ShouldBeEmpty();
        result.ExitCode.ShouldBe(0);

        //什么都没变
        GitletCli.Run(_dir, "log").Output.ShouldBe(logBefore);
        File.ReadAllText(Path.Combine(_dir, "a.txt")).ShouldBe("a");
    }

    //切换分支:当前分支变了
    [Theory]
    [InlineData("dev")]
    [InlineData("feature")]
    [InlineData("fix-1")]
    public void CheckoutBranch_SwitchesCurrentBranch(string name)
    {
        // Arrange
        GitletCli.Run(_dir, "init");

        GitletCli.Run(_dir, "branch", name);

        // Act
        var result = GitletCli.Run(_dir, "checkout", name);

        // Assert
        result.Error.ShouldBeEmpty();
        result.ExitCode.ShouldBe(0);

        //已经在新分支上了,main 可以切回去
        GitletCli.Run(_dir, "checkout", name).Output.ShouldBe(ExceptionMessages.ALreadyOnTheBranch + Environment.NewLine);

        var status = GitletCli.Run(_dir, "status").Output;
        status.ShouldContain($"*{name}\n");
        status.ShouldNotContain("*main");
    }

    //切换分支:工作区的文件换成目标分支的版本
    [Fact]
    public void CheckoutBranch_SwapsWorkingDirectoryFiles()
    {
        // Arrange
        GitletCli.Run(_dir, "init");

        File.WriteAllText(Path.Combine(_dir, "shared.txt"), "base");
        GitletCli.Run(_dir, "add", "shared.txt");
        GitletCli.Run(_dir, "commit", "base");

        GitletCli.Run(_dir, "branch", "dev");

        //dev 改 shared.txt,并新增 dev-only.txt
        GitletCli.Run(_dir, "checkout", "dev");
        File.WriteAllText(Path.Combine(_dir, "shared.txt"), "dev version");
        File.WriteAllText(Path.Combine(_dir, "dev-only.txt"), "d");
        GitletCli.Run(_dir, "add", "shared.txt");
        GitletCli.Run(_dir, "add", "dev-only.txt");
        GitletCli.Run(_dir, "commit", "dev edit");

        // Act
        GitletCli.Run(_dir, "checkout", "main");

        // Assert
        File.ReadAllText(Path.Combine(_dir, "shared.txt")).ShouldBe("base");
        File.Exists(Path.Combine(_dir, "dev-only.txt")).ShouldBeFalse();

        GitletCli.Run(_dir, "log").Output.ShouldNotContain("dev edit");

        // Act
        GitletCli.Run(_dir, "checkout", "dev");

        // Assert
        File.ReadAllText(Path.Combine(_dir, "shared.txt")).ShouldBe("dev version");
        File.ReadAllText(Path.Combine(_dir, "dev-only.txt")).ShouldBe("d");

        GitletCli.Run(_dir, "log").Output.ShouldContain("dev edit\n");
    }

    //切换之后的提交落在新分支上
    [Fact]
    public void CheckoutBranch_NewCommitsGoToTheCheckedOutBranch()
    {
        // Arrange
        GitletCli.Run(_dir, "init");

        var mainLog = GitletCli.Run(_dir, "log").Output;

        GitletCli.Run(_dir, "branch", "dev");

        // Act
        GitletCli.Run(_dir, "checkout", "dev");

        File.WriteAllText(Path.Combine(_dir, "a.txt"), "x");
        GitletCli.Run(_dir, "add", "a.txt");
        GitletCli.Run(_dir, "commit", "on dev");

        // Assert
        GitletCli.Run(_dir, "log").Output.ShouldContain("on dev\n");

        //main 没动
        GitletCli.Run(_dir, "checkout", "main");
        GitletCli.Run(_dir, "log").Output.ShouldBe(mainLog);
    }

    //切换分支会清空暂存区
    [Fact]
    public void CheckoutBranch_ClearsStaging()
    {
        // Arrange
        GitletCli.Run(_dir, "init");

        File.WriteAllText(Path.Combine(_dir, "old.txt"), "x");
        GitletCli.Run(_dir, "add", "old.txt");
        GitletCli.Run(_dir, "commit", "add old");

        GitletCli.Run(_dir, "branch", "dev");

        //暂存一个添加和一个删除
        File.WriteAllText(Path.Combine(_dir, "a.txt"), "x");
        GitletCli.Run(_dir, "add", "a.txt");
        GitletCli.Run(_dir, "rm", "old.txt");

        // Act
        GitletCli.Run(_dir, "checkout", "dev");

        // Assert
        //暂存区是空的,没东西可提交
        GitletCli.Run(_dir, "commit", "nothing").Output.ShouldBe(ExceptionMessages.NoChangesAdded + Environment.NewLine);

        var status = GitletCli.Run(_dir, "status").Output;
        status.ShouldContain("=== Staged Files ===\n\n");
        status.ShouldContain("=== Removed Files ===\n\n");
    }

    //目标分支里没有的未跟踪文件:不受影响
    [Fact]
    public void CheckoutBranch_UntrackedFileNotInTargetBranch_IsLeftAlone()
    {
        // Arrange
        GitletCli.Run(_dir, "init");

        GitletCli.Run(_dir, "branch", "dev");

        File.WriteAllText(Path.Combine(_dir, "notes.txt"), "untracked");

        // Act
        GitletCli.Run(_dir, "checkout", "dev");

        // Assert
        File.ReadAllText(Path.Combine(_dir, "notes.txt")).ShouldBe("untracked");

        GitletCli.Run(_dir, "checkout", "dev").Output.ShouldBe(ExceptionMessages.ALreadyOnTheBranch + Environment.NewLine);
    }

    //未跟踪文件挡路:内容相同、不同、为空都不允许覆盖
    [Theory]
    [InlineData("tracked on dev")]
    [InlineData("untracked on main")]
    [InlineData("")]
    public void CheckoutBranch_UntrackedFileInTheWay_PrintsMessageAndKeepsFile(string untrackedContent)
    {
        // Arrange
        GitletCli.Run(_dir, "init");

        GitletCli.Run(_dir, "branch", "dev");

        //dev 跟踪 a.txt
        GitletCli.Run(_dir, "checkout", "dev");
        File.WriteAllText(Path.Combine(_dir, "a.txt"), "tracked on dev");
        GitletCli.Run(_dir, "add", "a.txt");
        GitletCli.Run(_dir, "commit", "dev file");

        GitletCli.Run(_dir, "checkout", "main");

        //main 的工作区放一个未跟踪的同名文件
        File.WriteAllText(Path.Combine(_dir, "a.txt"), untrackedContent);

        // Act
        var result = GitletCli.Run(_dir, "checkout", "dev");

        // Assert
        result.Output.ShouldBe(ExceptionMessages.UntrackedFileInTheWay + Environment.NewLine);

        //文件没被覆盖,分支也没切换
        File.ReadAllText(Path.Combine(_dir, "a.txt")).ShouldBe(untrackedContent);
        GitletCli.Run(_dir, "checkout", "main").Output.ShouldBe(ExceptionMessages.ALreadyOnTheBranch + Environment.NewLine);
    }

}
