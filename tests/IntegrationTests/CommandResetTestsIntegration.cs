using System.Text.RegularExpressions;
using Shouldly;

namespace Gitlet.Tests.IntegrationTests;

public class CommandResetTestsIntegration : IDisposable
{

    private readonly string _dir = Path.Combine(Path.GetTempPath(), "gitlet-reset-cli-" + Guid.NewGuid());

    //constructor
    public CommandResetTestsIntegration()
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
    [InlineData("reset")]
    [InlineData("reset", "abc", "extra")]
    public void Reset_WrongNumberOfArguments_IsIncorrectOperands(params string[] args)
    {
        // Arrange
        GitletCli.Run(_dir, "init");

        File.WriteAllText(Path.Combine(_dir, "a.txt"), "v1");
        GitletCli.Run(_dir, "add", "a.txt");
        GitletCli.Run(_dir, "commit", "first");

        var logBefore = GitletCli.Run(_dir, "log").Output;

        // Act
        var result = GitletCli.Run(_dir, args);

        // Assert
        result.Output.ShouldBe(ExceptionMessages.IncorrectOperands + Environment.NewLine);

        GitletCli.Run(_dir, "log").Output.ShouldBe(logBefore);
    }

    //还没 init 就 reset
    [Fact]
    public void Reset_BeforeInit_IsRejected()
    {
        // Act
        var result = GitletCli.Run(_dir, "reset", "abc");

        // Assert
        result.Output.ShouldBe(ExceptionMessages.NotInitialized + Environment.NewLine);

        Directory.Exists(Path.Combine(_dir, ".gitlet")).ShouldBeFalse();
    }

    //什么都不打印,分支和工作区都回到那个 commit
    [Theory]
    [InlineData(40)] //完整哈希
    [InlineData(8)]  //缩写哈希
    [InlineData(6)]
    public void Reset_PrintsNothingAndMovesBackToThatCommit(int idLength)
    {
        // Arrange
        GitletCli.Run(_dir, "init");

        File.WriteAllText(Path.Combine(_dir, "a.txt"), "v1");
        GitletCli.Run(_dir, "add", "a.txt");
        GitletCli.Run(_dir, "commit", "first");

        var logAtFirst = GitletCli.Run(_dir, "log").Output;
        var firstId = Regex.Match(logAtFirst, @"commit\s+([0-9a-f]{40})").Groups[1].Value;

        File.WriteAllText(Path.Combine(_dir, "a.txt"), "v2");
        GitletCli.Run(_dir, "add", "a.txt");
        GitletCli.Run(_dir, "commit", "second");

        File.WriteAllText(Path.Combine(_dir, "b.txt"), "b");
        GitletCli.Run(_dir, "add", "b.txt");
        GitletCli.Run(_dir, "commit", "third");

        // Act
        var result = GitletCli.Run(_dir, "reset", firstId[..idLength]);

        // Assert
        result.Output.ShouldBeEmpty();
        result.Error.ShouldBeEmpty();
        result.ExitCode.ShouldBe(0);

        //工作区:a.txt 回到 v1,后来才有的 b.txt 被删掉
        File.ReadAllText(Path.Combine(_dir, "a.txt")).ShouldBe("v1");
        File.Exists(Path.Combine(_dir, "b.txt")).ShouldBeFalse();

        //历史和当时一模一样
        GitletCli.Run(_dir, "log").Output.ShouldBe(logAtFirst);
    }

    //被甩掉的 commit 还在 global-log 里,可以再 reset 回去
    [Fact]
    public void Reset_KeepsLaterCommitsAndCanGoForwardAgain()
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

        var logAtSecond = GitletCli.Run(_dir, "log").Output;
        var secondId = Regex.Match(logAtSecond, @"commit\s+([0-9a-f]{40})").Groups[1].Value;

        // Act
        GitletCli.Run(_dir, "reset", firstId);

        // Assert
        GitletCli.Run(_dir, "log").Output.ShouldNotContain("second");
        GitletCli.Run(_dir, "global-log").Output.ShouldContain("second\n");

        // Act
        GitletCli.Run(_dir, "reset", secondId);

        // Assert
        File.ReadAllText(Path.Combine(_dir, "a.txt")).ShouldBe("v2");
        GitletCli.Run(_dir, "log").Output.ShouldBe(logAtSecond);
    }

    //reset 只移动当前分支
    [Fact]
    public void Reset_MovesOnlyTheCurrentBranch()
    {
        // Arrange
        GitletCli.Run(_dir, "init");

        var initialId = Regex.Match(GitletCli.Run(_dir, "log").Output, @"commit\s+([0-9a-f]{40})").Groups[1].Value;

        File.WriteAllText(Path.Combine(_dir, "a.txt"), "v1");
        GitletCli.Run(_dir, "add", "a.txt");
        GitletCli.Run(_dir, "commit", "first");

        var logAtFirst = GitletCli.Run(_dir, "log").Output;

        GitletCli.Run(_dir, "branch", "dev");

        // Act
        GitletCli.Run(_dir, "reset", initialId);

        // Assert
        //还在 main 上,main 回到了初始 commit
        GitletCli.Run(_dir, "checkout", "main").Output.ShouldBe(ExceptionMessages.ALreadyOnTheBranch + Environment.NewLine);
        GitletCli.Run(_dir, "log").Output.ShouldNotContain("first");

        //dev 没动
        GitletCli.Run(_dir, "checkout", "dev");
        GitletCli.Run(_dir, "log").Output.ShouldBe(logAtFirst);
    }

    //reset 会清空暂存区
    [Fact]
    public void Reset_ClearsStaging()
    {
        // Arrange
        GitletCli.Run(_dir, "init");

        File.WriteAllText(Path.Combine(_dir, "a.txt"), "v1");
        File.WriteAllText(Path.Combine(_dir, "old.txt"), "old");
        GitletCli.Run(_dir, "add", "a.txt");
        GitletCli.Run(_dir, "add", "old.txt");
        GitletCli.Run(_dir, "commit", "first");

        var firstId = Regex.Match(GitletCli.Run(_dir, "log").Output, @"commit\s+([0-9a-f]{40})").Groups[1].Value;

        //暂存一个添加和一个删除
        File.WriteAllText(Path.Combine(_dir, "a.txt"), "v2");
        GitletCli.Run(_dir, "add", "a.txt");
        GitletCli.Run(_dir, "rm", "old.txt");

        // Act
        GitletCli.Run(_dir, "reset", firstId);

        // Assert
        File.ReadAllText(Path.Combine(_dir, "a.txt")).ShouldBe("v1");
        File.ReadAllText(Path.Combine(_dir, "old.txt")).ShouldBe("old");

        //暂存区是空的,没东西可提交
        GitletCli.Run(_dir, "commit", "nothing").Output.ShouldBe(ExceptionMessages.NoChangesAdded + Environment.NewLine);
    }

    //commit 不存在
    [Theory]
    [InlineData("zzzzzzzz")] //不是合法的哈希
    [InlineData("nope")]
    public void Reset_UnknownCommit_PrintsMessageAndChangesNothing(string commitId)
    {
        // Arrange
        GitletCli.Run(_dir, "init");

        File.WriteAllText(Path.Combine(_dir, "a.txt"), "v1");
        GitletCli.Run(_dir, "add", "a.txt");
        GitletCli.Run(_dir, "commit", "first");

        var logBefore = GitletCli.Run(_dir, "log").Output;

        // Act
        var result = GitletCli.Run(_dir, "reset", commitId);

        // Assert
        result.Output.ShouldBe(ExceptionMessages.CommitDoesNotExist + Environment.NewLine);
        result.Error.ShouldBeEmpty();
        result.ExitCode.ShouldBe(0);

        GitletCli.Run(_dir, "log").Output.ShouldBe(logBefore);
        File.ReadAllText(Path.Combine(_dir, "a.txt")).ShouldBe("v1");
    }

    //未跟踪文件挡路:内容相同、不同、为空都不允许覆盖
    [Theory]
    [InlineData("v1")]
    [InlineData("untracked now")]
    [InlineData("")]
    public void Reset_UntrackedFileInTheWay_PrintsMessageAndKeepsFile(string untrackedContent)
    {
        // Arrange
        GitletCli.Run(_dir, "init");

        File.WriteAllText(Path.Combine(_dir, "a.txt"), "v1");
        GitletCli.Run(_dir, "add", "a.txt");
        GitletCli.Run(_dir, "commit", "first");

        var firstId = Regex.Match(GitletCli.Run(_dir, "log").Output, @"commit\s+([0-9a-f]{40})").Groups[1].Value;

        //之后的 commit 不再跟踪 a.txt
        GitletCli.Run(_dir, "rm", "a.txt");
        GitletCli.Run(_dir, "commit", "remove a");

        var logBefore = GitletCli.Run(_dir, "log").Output;

        //工作区放一个未跟踪的同名文件
        File.WriteAllText(Path.Combine(_dir, "a.txt"), untrackedContent);

        // Act
        var result = GitletCli.Run(_dir, "reset", firstId);

        // Assert
        result.Output.ShouldBe(ExceptionMessages.UntrackedFileInTheWay + Environment.NewLine);

        //文件没被覆盖,分支也没移动
        File.ReadAllText(Path.Combine(_dir, "a.txt")).ShouldBe(untrackedContent);
        GitletCli.Run(_dir, "log").Output.ShouldBe(logBefore);
    }

}
