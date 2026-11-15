using Shouldly;

namespace Gitlet.Tests.IntegrationTests;

public class CommandStatusTestsIntegration : IDisposable
{

    private readonly string _dir = Path.Combine(Path.GetTempPath(), "gitlet-status-cli-" + Guid.NewGuid());

    //constructor
    public CommandStatusTestsIntegration()
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
    [InlineData("status", "extra")]
    [InlineData("status", "extra", "more")]
    public void Status_WrongNumberOfArguments_IsIncorrectOperands(params string[] args)
    {
        // Arrange
        GitletCli.Run(_dir, "init");

        // Act
        var result = GitletCli.Run(_dir, args);

        // Assert
        result.Output.ShouldBe(ExceptionMessages.IncorrectOperands + Environment.NewLine);
    }

    //还没 init 就 status
    [Fact]
    public void Status_BeforeInit_IsRejected()
    {
        // Act
        var result = GitletCli.Run(_dir, "status");

        // Assert
        result.Output.ShouldBe(ExceptionMessages.NotInitialized + Environment.NewLine);

        Directory.Exists(Path.Combine(_dir, ".gitlet")).ShouldBeFalse();
    }

    //刚 init 的仓库:五个区块,只有 main
    [Fact]
    public void Status_FreshRepository_PrintsExactLayoutToStdout()
    {
        // Arrange
        GitletCli.Run(_dir, "init");

        // Act
        var result = GitletCli.Run(_dir, "status");

        // Assert
        result.Output.ShouldBe(
            "=== Branches ===\n*main\n\n" +
            "=== Staged Files ===\n\n" +
            "=== Removed Files ===\n\n" +
            "=== Modifications Not Staged For Commit ===\n\n" +
            "=== Untracked Files ===\n\n");
        result.Error.ShouldBeEmpty();
        result.ExitCode.ShouldBe(0);
    }

    //分支按名字排序,当前分支前面有 *
    [Theory]
    [InlineData("main", "=== Branches ===\nalpha\n*main\nzeta\n\n")]
    [InlineData("alpha", "=== Branches ===\n*alpha\nmain\nzeta\n\n")]
    [InlineData("zeta", "=== Branches ===\nalpha\nmain\n*zeta\n\n")]
    public void Status_BranchesAreSortedAndCurrentOneIsMarked(string currentBranch, string expectedStart)
    {
        // Arrange
        GitletCli.Run(_dir, "init");

        GitletCli.Run(_dir, "branch", "zeta");
        GitletCli.Run(_dir, "branch", "alpha");

        if (currentBranch != "main")
        {
            GitletCli.Run(_dir, "checkout", currentBranch);
        }

        // Act
        var result = GitletCli.Run(_dir, "status");

        // Assert
        result.Output.ShouldStartWith(expectedStart);
    }

    //暂存的添加和删除各自显示,按文件名排序
    [Fact]
    public void Status_ShowsStagedAndRemovedFilesSorted()
    {
        // Arrange
        GitletCli.Run(_dir, "init");

        File.WriteAllText(Path.Combine(_dir, "old2.txt"), "x");
        File.WriteAllText(Path.Combine(_dir, "old1.txt"), "x");
        GitletCli.Run(_dir, "add", "old2.txt");
        GitletCli.Run(_dir, "add", "old1.txt");
        GitletCli.Run(_dir, "commit", "add old files");

        //删两个旧文件
        GitletCli.Run(_dir, "rm", "old2.txt");
        GitletCli.Run(_dir, "rm", "old1.txt");

        //加两个新文件
        File.WriteAllText(Path.Combine(_dir, "new2.txt"), "y");
        File.WriteAllText(Path.Combine(_dir, "new1.txt"), "y");
        GitletCli.Run(_dir, "add", "new2.txt");
        GitletCli.Run(_dir, "add", "new1.txt");

        // Act
        var result = GitletCli.Run(_dir, "status");

        // Assert
        result.Output.ShouldBe(
            "=== Branches ===\n*main\n\n" +
            "=== Staged Files ===\nnew1.txt\nnew2.txt\n\n" +
            "=== Removed Files ===\nold1.txt\nold2.txt\n\n" +
            "=== Modifications Not Staged For Commit ===\n\n" +
            "=== Untracked Files ===\n\n");
    }

    //commit 之后两个区块都清空
    [Fact]
    public void Status_AfterCommit_StagingSectionsAreEmpty()
    {
        // Arrange
        GitletCli.Run(_dir, "init");

        File.WriteAllText(Path.Combine(_dir, "a.txt"), "x");
        GitletCli.Run(_dir, "add", "a.txt");
        GitletCli.Run(_dir, "commit", "first");

        // Act
        var result = GitletCli.Run(_dir, "status");

        // Assert
        result.Output.ShouldContain("=== Staged Files ===\n\n");
        result.Output.ShouldContain("=== Removed Files ===\n\n");
    }

    //status 不改变任何东西:连续两次输出一样
    [Fact]
    public void Status_Twice_PrintsTheSame()
    {
        // Arrange
        GitletCli.Run(_dir, "init");

        File.WriteAllText(Path.Combine(_dir, "a.txt"), "x");
        GitletCli.Run(_dir, "add", "a.txt");

        // Act
        var first = GitletCli.Run(_dir, "status").Output;
        var second = GitletCli.Run(_dir, "status").Output;

        // Assert
        first.ShouldContain("=== Staged Files ===\na.txt\n\n");
        second.ShouldBe(first);
    }

}
