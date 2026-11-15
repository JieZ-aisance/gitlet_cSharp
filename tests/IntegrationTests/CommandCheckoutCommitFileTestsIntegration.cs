using System.Text.RegularExpressions;
using Shouldly;

namespace Gitlet.Tests.IntegrationTests;

public class CommandCheckoutCommitFileTestsIntegration : IDisposable
{

    private readonly string _dir = Path.Combine(Path.GetTempPath(), "gitlet-checkoutcommitfile-cli-" + Guid.NewGuid());

    //constructor
    public CommandCheckoutCommitFileTestsIntegration()
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

    //参数形状不对:-- 必须在 commit id 和文件名中间
    [Theory]
    [InlineData("++")]  //分隔符不是 --
    [InlineData("a.txt")]
    public void CheckoutCommitFile_WrongSeparator_IsIncorrectOperands(string separator)
    {
        // Arrange
        GitletCli.Run(_dir, "init");

        File.WriteAllText(Path.Combine(_dir, "a.txt"), "v1");
        GitletCli.Run(_dir, "add", "a.txt");
        GitletCli.Run(_dir, "commit", "first");

        var firstId = Regex.Match(GitletCli.Run(_dir, "log").Output, @"commit\s+([0-9a-f]{40})").Groups[1].Value;

        File.WriteAllText(Path.Combine(_dir, "a.txt"), "scribbled");

        // Act
        var result = GitletCli.Run(_dir, "checkout", firstId, separator, "a.txt");

        // Assert
        result.Output.ShouldBe(ExceptionMessages.IncorrectOperands + Environment.NewLine);

        File.ReadAllText(Path.Combine(_dir, "a.txt")).ShouldBe("scribbled");
    }

    //还没 init 就 checkout
    [Fact]
    public void CheckoutCommitFile_BeforeInit_IsRejected()
    {
        // Act
        var result = GitletCli.Run(_dir, "checkout", "abc123", "--", "a.txt");

        // Assert
        result.Output.ShouldBe(ExceptionMessages.NotInitialized + Environment.NewLine);

        Directory.Exists(Path.Combine(_dir, ".gitlet")).ShouldBeFalse();
    }

    //什么都不打印,把文件恢复成那个 commit 里的版本
    [Theory]
    [InlineData(40)] //完整哈希
    [InlineData(8)]  //缩写哈希
    [InlineData(6)]
    public void CheckoutCommitFile_PrintsNothingAndRestoresVersionFromThatCommit(int idLength)
    {
        // Arrange
        GitletCli.Run(_dir, "init");

        File.WriteAllText(Path.Combine(_dir, "a.txt"), "v1");
        GitletCli.Run(_dir, "add", "a.txt");
        GitletCli.Run(_dir, "commit", "first");

        var firstId = Regex.Match(GitletCli.Run(_dir, "log").Output, @"commit\s+([0-9a-f]{40})").Groups[1].Value;

        File.WriteAllText(Path.Combine(_dir, "a.txt"), "v2");
        GitletCli.Run(_dir, "add", "a.txt");
        GitletCli.Run(_dir, "commit", "second");

        // Act
        var result = GitletCli.Run(_dir, "checkout", firstId[..idLength], "--", "a.txt");

        // Assert
        result.Output.ShouldBeEmpty();
        result.Error.ShouldBeEmpty();
        result.ExitCode.ShouldBe(0);

        File.ReadAllText(Path.Combine(_dir, "a.txt")).ShouldBe("v1");
    }

    //只改工作区:分支不动,暂存区不动
    [Fact]
    public void CheckoutCommitFile_DoesNotMoveBranchOrStage()
    {
        // Arrange
        GitletCli.Run(_dir, "init");

        File.WriteAllText(Path.Combine(_dir, "a.txt"), "v1");
        GitletCli.Run(_dir, "add", "a.txt");
        GitletCli.Run(_dir, "commit", "first");

        var firstId = Regex.Match(GitletCli.Run(_dir, "log").Output, @"commit\s+([0-9a-f]{40})").Groups[1].Value;

        File.WriteAllText(Path.Combine(_dir, "a.txt"), "v2");
        GitletCli.Run(_dir, "add", "a.txt");
        GitletCli.Run(_dir, "commit", "second");

        var logBefore = GitletCli.Run(_dir, "log").Output;

        // Act
        GitletCli.Run(_dir, "checkout", firstId, "--", "a.txt");

        // Assert
        File.ReadAllText(Path.Combine(_dir, "a.txt")).ShouldBe("v1");

        GitletCli.Run(_dir, "log").Output.ShouldBe(logBefore);

        //暂存区是空的,没东西可提交
        GitletCli.Run(_dir, "commit", "nothing").Output.ShouldBe(ExceptionMessages.NoChangesAdded + Environment.NewLine);
    }

    //后来被删掉的文件,可以从旧 commit 里拿回来
    [Fact]
    public void CheckoutCommitFile_FileRemovedLater_RecreatesIt()
    {
        // Arrange
        GitletCli.Run(_dir, "init");

        File.WriteAllText(Path.Combine(_dir, "a.txt"), "v1");
        GitletCli.Run(_dir, "add", "a.txt");
        GitletCli.Run(_dir, "commit", "first");

        var firstId = Regex.Match(GitletCli.Run(_dir, "log").Output, @"commit\s+([0-9a-f]{40})").Groups[1].Value;

        GitletCli.Run(_dir, "rm", "a.txt");
        GitletCli.Run(_dir, "commit", "remove a");

        // Act
        GitletCli.Run(_dir, "checkout", firstId, "--", "a.txt");

        // Assert
        File.ReadAllText(Path.Combine(_dir, "a.txt")).ShouldBe("v1");
    }

    //其他分支上的 commit 也可以用
    [Fact]
    public void CheckoutCommitFile_CommitFromAnotherBranch_RestoresItsVersion()
    {
        // Arrange
        GitletCli.Run(_dir, "init");

        GitletCli.Run(_dir, "branch", "dev");

        //dev
        GitletCli.Run(_dir, "checkout", "dev");
        File.WriteAllText(Path.Combine(_dir, "a.txt"), "dev version");
        GitletCli.Run(_dir, "add", "a.txt");
        GitletCli.Run(_dir, "commit", "on dev");

        var devId = Regex.Match(GitletCli.Run(_dir, "log").Output, @"commit\s+([0-9a-f]{40})").Groups[1].Value;

        GitletCli.Run(_dir, "checkout", "main");

        // Act
        GitletCli.Run(_dir, "checkout", devId, "--", "a.txt");

        // Assert
        File.ReadAllText(Path.Combine(_dir, "a.txt")).ShouldBe("dev version");

        //还在 main 上
        GitletCli.Run(_dir, "checkout", "main").Output.ShouldBe(ExceptionMessages.ALreadyOnTheBranch + Environment.NewLine);
    }

    //commit 不存在
    [Theory]
    [InlineData("zzzzzzzz")] //不是合法的哈希
    [InlineData("nope")]
    public void CheckoutCommitFile_UnknownCommit_PrintsOnlyTheMessage(string commitId)
    {
        // Arrange
        GitletCli.Run(_dir, "init");

        File.WriteAllText(Path.Combine(_dir, "a.txt"), "v1");
        GitletCli.Run(_dir, "add", "a.txt");
        GitletCli.Run(_dir, "commit", "first");

        File.WriteAllText(Path.Combine(_dir, "a.txt"), "scribbled");

        // Act
        var result = GitletCli.Run(_dir, "checkout", commitId, "--", "a.txt");

        // Assert
        result.Output.ShouldBe(ExceptionMessages.CommitDoesNotExist + Environment.NewLine);
        result.Error.ShouldBeEmpty();
        result.ExitCode.ShouldBe(0);

        File.ReadAllText(Path.Combine(_dir, "a.txt")).ShouldBe("scribbled");
    }

    //commit 存在,但里面没有这个文件
    [Fact]
    public void CheckoutCommitFile_FileNotInThatCommit_PrintsOnlyTheMessage()
    {
        // Arrange
        GitletCli.Run(_dir, "init");

        var initialId = Regex.Match(GitletCli.Run(_dir, "log").Output, @"commit\s+([0-9a-f]{40})").Groups[1].Value;

        File.WriteAllText(Path.Combine(_dir, "a.txt"), "v1");
        GitletCli.Run(_dir, "add", "a.txt");
        GitletCli.Run(_dir, "commit", "first");

        // Act
        var result = GitletCli.Run(_dir, "checkout", initialId, "--", "a.txt");

        // Assert
        result.Output.ShouldBe(ExceptionMessages.FileDoesNotExist + Environment.NewLine);
        result.Error.ShouldBeEmpty();

        //工作区的文件没被动过
        File.ReadAllText(Path.Combine(_dir, "a.txt")).ShouldBe("v1");
    }

}
