using Shouldly;

namespace Gitlet.Tests.IntegrationTests;

public class CommandRmBranchTestsIntegration : IDisposable
{

    private readonly string _dir = Path.Combine(Path.GetTempPath(), "gitlet-rmbranch-cli-" + Guid.NewGuid());

    //constructor
    public CommandRmBranchTestsIntegration()
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

    //参数个数不对
    [Theory]
    [InlineData("rm-branch")]
    [InlineData("rm-branch", "dev", "extra")]
    public void RmBranch_WrongNumberOfArguments_IsIncorrectOperands(params string[] args)
    {
        // Arrange
        GitletCli.Run(_dir, "init");

        GitletCli.Run(_dir, "branch", "dev");

        // Act
        var result = GitletCli.Run(_dir, args);

        // Assert
        result.Output.ShouldBe(ExceptionMessages.IncorrectOperands + Environment.NewLine);

        //dev 还在
        GitletCli.Run(_dir, "branch", "dev").Output.ShouldBe(ExceptionMessages.BranchAlreadyExists + Environment.NewLine);
    }

    //还没 init 就 rm-branch
    [Fact]
    public void RmBranch_BeforeInit_IsRejected()
    {
        // Act
        var result = GitletCli.Run(_dir, "rm-branch", "dev");

        // Assert
        result.Output.ShouldBe(ExceptionMessages.NotInitialized + Environment.NewLine);

        Directory.Exists(Path.Combine(_dir, ".gitlet")).ShouldBeFalse();
    }

    //正常删分支:什么都不打印,分支没了
    [Theory]
    [InlineData("dev")]
    [InlineData("feature")]
    [InlineData("fix-1")]
    public void RmBranch_PrintsNothingAndRemovesTheBranch(string name)
    {
        // Arrange
        GitletCli.Run(_dir, "init");

        GitletCli.Run(_dir, "branch", name);

        // Act
        var result = GitletCli.Run(_dir, "rm-branch", name);

        // Assert
        result.Output.ShouldBeEmpty();
        result.Error.ShouldBeEmpty();
        result.ExitCode.ShouldBe(0);

        //切不过去了
        GitletCli.Run(_dir, "checkout", name).Output.ShouldBe(ExceptionMessages.BranchNoExists + Environment.NewLine);

        GitletCli.Run(_dir, "status").Output.ShouldStartWith("=== Branches ===\n*main\n\n");
    }

    //只删指针:分支上的 commit 还在,工作区也不变
    [Fact]
    public void RmBranch_KeepsTheCommitsAndTheWorkingDirectory()
    {
        // Arrange
        GitletCli.Run(_dir, "init");

        File.WriteAllText(Path.Combine(_dir, "base.txt"), "base");
        GitletCli.Run(_dir, "add", "base.txt");
        GitletCli.Run(_dir, "commit", "base");

        GitletCli.Run(_dir, "branch", "dev");

        //dev
        GitletCli.Run(_dir, "checkout", "dev");
        File.WriteAllText(Path.Combine(_dir, "dev.txt"), "dev");
        GitletCli.Run(_dir, "add", "dev.txt");
        GitletCli.Run(_dir, "commit", "only on dev");

        GitletCli.Run(_dir, "checkout", "main");

        var logBefore = GitletCli.Run(_dir, "log").Output;

        // Act
        GitletCli.Run(_dir, "rm-branch", "dev");

        // Assert
        GitletCli.Run(_dir, "global-log").Output.ShouldContain("only on dev\n");

        //main 的历史和文件都没变
        GitletCli.Run(_dir, "log").Output.ShouldBe(logBefore);
        File.ReadAllText(Path.Combine(_dir, "base.txt")).ShouldBe("base");
    }

    //删掉之后可以再建同名分支
    [Fact]
    public void RmBranch_NameCanBeCreatedAgain()
    {
        // Arrange
        GitletCli.Run(_dir, "init");

        GitletCli.Run(_dir, "branch", "dev");
        GitletCli.Run(_dir, "rm-branch", "dev");

        // Act
        var result = GitletCli.Run(_dir, "branch", "dev");

        // Assert
        result.Output.ShouldBeEmpty();

        //能切过去,说明分支又存在了
        GitletCli.Run(_dir, "checkout", "dev");
        GitletCli.Run(_dir, "checkout", "dev").Output.ShouldBe(ExceptionMessages.ALreadyOnTheBranch + Environment.NewLine);
    }

    //分支不存在
    [Fact]
    public void RmBranch_Unknown_PrintsOnlyTheMessage()
    {
        // Arrange
        GitletCli.Run(_dir, "init");

        // Act
        var result = GitletCli.Run(_dir, "rm-branch", "ghost");

        // Assert
        result.Output.ShouldBe(ExceptionMessages.BranchNoExists + Environment.NewLine);
        result.Error.ShouldBeEmpty();
        result.ExitCode.ShouldBe(0);
    }

    //不能删当前分支
    [Theory]
    [InlineData("main")]
    [InlineData("dev")]
    public void RmBranch_CurrentBranch_PrintsMessageAndKeepsTheBranch(string currentBranch)
    {
        // Arrange
        GitletCli.Run(_dir, "init");

        GitletCli.Run(_dir, "branch", "dev");

        if (currentBranch != "main")
        {
            GitletCli.Run(_dir, "checkout", currentBranch);
        }

        // Act
        var result = GitletCli.Run(_dir, "rm-branch", currentBranch);

        // Assert
        result.Output.ShouldBe(ExceptionMessages.DeleteCurrentBranchForbiden + Environment.NewLine);

        //分支还在,而且还是当前分支
        GitletCli.Run(_dir, "checkout", currentBranch).Output.ShouldBe(ExceptionMessages.ALreadyOnTheBranch + Environment.NewLine);
    }

}
