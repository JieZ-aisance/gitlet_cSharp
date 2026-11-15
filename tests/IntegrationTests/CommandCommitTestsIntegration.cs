using System.Text.RegularExpressions;
using Shouldly;

namespace Gitlet.Tests.IntegrationTests;

public class CommandCommitTestsIntegration : IDisposable
{

    private readonly string _dir = Path.Combine(Path.GetTempPath(), "gitlet-commit-cli-" + Guid.NewGuid());

    //constructor
    public CommandCommitTestsIntegration()
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

    //参数个数不对:消息没加引号被拆成多个参数也算
    [Theory]
    [InlineData("commit")]
    [InlineData("commit", "one", "two")]
    [InlineData("commit", "one", "two", "three")]
    public void Commit_WrongNumberOfArguments_IsIncorrectOperands(params string[] args)
    {
        // Arrange
        GitletCli.Run(_dir, "init");

        File.WriteAllText(Path.Combine(_dir, "a.txt"), "x");
        GitletCli.Run(_dir, "add", "a.txt");

        var logBefore = GitletCli.Run(_dir, "log").Output;

        // Act
        var result = GitletCli.Run(_dir, args);

        // Assert
        result.Output.ShouldBe(ExceptionMessages.IncorrectOperands + Environment.NewLine);

        GitletCli.Run(_dir, "log").Output.ShouldBe(logBefore);
    }

    //还没 init 就 commit
    [Fact]
    public void Commit_BeforeInit_IsRejected()
    {
        // Act
        var result = GitletCli.Run(_dir, "commit", "message");

        // Assert
        result.Output.ShouldBe(ExceptionMessages.NotInitialized + Environment.NewLine);

        Directory.Exists(Path.Combine(_dir, ".gitlet")).ShouldBeFalse();
    }

    //消息为空
    [Fact]
    public void Commit_EmptyMessage_PrintsOnlyTheMessage()
    {
        // Arrange
        GitletCli.Run(_dir, "init");

        File.WriteAllText(Path.Combine(_dir, "a.txt"), "x");
        GitletCli.Run(_dir, "add", "a.txt");

        var logBefore = GitletCli.Run(_dir, "log").Output;

        // Act
        var result = GitletCli.Run(_dir, "commit", "");

        // Assert
        result.Output.ShouldBe(ExceptionMessages.PleaseEnterCommitMessage + Environment.NewLine);
        result.Error.ShouldBeEmpty();
        result.ExitCode.ShouldBe(0);

        GitletCli.Run(_dir, "log").Output.ShouldBe(logBefore);
    }

    //暂存区是空的
    [Fact]
    public void Commit_NothingStaged_PrintsOnlyTheMessage()
    {
        // Arrange
        GitletCli.Run(_dir, "init");

        //工作区有文件,但没 add
        File.WriteAllText(Path.Combine(_dir, "a.txt"), "x");

        var logBefore = GitletCli.Run(_dir, "log").Output;

        // Act
        var result = GitletCli.Run(_dir, "commit", "empty");

        // Assert
        result.Output.ShouldBe(ExceptionMessages.NoChangesAdded + Environment.NewLine);
        result.Error.ShouldBeEmpty();

        GitletCli.Run(_dir, "log").Output.ShouldBe(logBefore);
    }

    //正常 commit:什么都不打印,log 最上面是新的 commit
    [Theory]
    [InlineData("first")]
    [InlineData("a message with spaces")]
    [InlineData("修复 bug")]
    public void Commit_PrintsNothingAndAppearsOnTopOfLog(string message)
    {
        // Arrange
        GitletCli.Run(_dir, "init");

        var initialLog = GitletCli.Run(_dir, "log").Output;

        File.WriteAllText(Path.Combine(_dir, "a.txt"), "x");
        GitletCli.Run(_dir, "add", "a.txt");

        // Act
        var result = GitletCli.Run(_dir, "commit", message);

        // Assert
        result.Output.ShouldBeEmpty();
        result.Error.ShouldBeEmpty();

        var log = GitletCli.Run(_dir, "log").Output;

        //新的 commit 在最上面,原来的历史接在后面
        log.ShouldContain($"\n{message}\n");
        log.ShouldEndWith(initialLog);
        Regex.Matches(log, @"commit\s+([0-9a-f]{40})").Count.ShouldBe(2);
    }

    //连续提交:每次多一条,最新的在最上面
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(5)]
    public void Commit_SeveralTimes_AddsOneLogEntryEach(int count)
    {
        // Arrange
        GitletCli.Run(_dir, "init");

        // Act
        for (var i = 1; i <= count; i++)
        {
            File.WriteAllText(Path.Combine(_dir, "a.txt"), $"v{i}");
            GitletCli.Run(_dir, "add", "a.txt");
            GitletCli.Run(_dir, "commit", $"message {i}");
        }

        // Assert
        var log = GitletCli.Run(_dir, "log").Output;

        var hashes = Regex.Matches(log, @"commit\s+([0-9a-f]{40})").Select(m => m.Groups[1].Value).ToList();

        hashes.Count.ShouldBe(count + 1);
        hashes.Distinct().Count().ShouldBe(count + 1);

        //最新的在最上面,初始 commit 在最下面
        log.IndexOf($"message {count}\n", StringComparison.Ordinal)
            .ShouldBeLessThanOrEqualTo(log.IndexOf("message 1\n", StringComparison.Ordinal));
        log.ShouldEndWith("initial commit\n");
    }

    //commit 之后暂存区被清空
    [Fact]
    public void Commit_ClearsStaging()
    {
        // Arrange
        GitletCli.Run(_dir, "init");

        File.WriteAllText(Path.Combine(_dir, "a.txt"), "x");
        GitletCli.Run(_dir, "add", "a.txt");

        // Act
        GitletCli.Run(_dir, "commit", "first");

        // Assert
        //马上再提交一次:没东西可提交
        GitletCli.Run(_dir, "commit", "second").Output.ShouldBe(ExceptionMessages.NoChangesAdded + Environment.NewLine);

        var status = GitletCli.Run(_dir, "status").Output;
        status.ShouldContain("=== Staged Files ===\n\n");
        status.ShouldContain("=== Removed Files ===\n\n");
    }

    //新的 commit 保留之前 commit 里的文件
    [Fact]
    public void Commit_KeepsFilesFromPreviousCommit()
    {
        // Arrange
        GitletCli.Run(_dir, "init");

        File.WriteAllText(Path.Combine(_dir, "a.txt"), "a1");
        GitletCli.Run(_dir, "add", "a.txt");
        GitletCli.Run(_dir, "commit", "add a");

        File.WriteAllText(Path.Combine(_dir, "b.txt"), "b1");
        GitletCli.Run(_dir, "add", "b.txt");

        // Act
        GitletCli.Run(_dir, "commit", "add b");

        // Assert
        //两个文件都能从最新的 commit 里恢复
        File.Delete(Path.Combine(_dir, "a.txt"));
        File.Delete(Path.Combine(_dir, "b.txt"));

        GitletCli.Run(_dir, "checkout", "--", "a.txt");
        GitletCli.Run(_dir, "checkout", "--", "b.txt");

        File.ReadAllText(Path.Combine(_dir, "a.txt")).ShouldBe("a1");
        File.ReadAllText(Path.Combine(_dir, "b.txt")).ShouldBe("b1");
    }

    //只有删除、没有添加,也可以提交
    [Fact]
    public void Commit_OnlyStagedRemoval_DropsFileFromCommit()
    {
        // Arrange
        GitletCli.Run(_dir, "init");

        File.WriteAllText(Path.Combine(_dir, "a.txt"), "a1");
        GitletCli.Run(_dir, "add", "a.txt");
        GitletCli.Run(_dir, "commit", "add a");

        GitletCli.Run(_dir, "rm", "a.txt");

        // Act
        var result = GitletCli.Run(_dir, "commit", "remove a");

        // Assert
        result.Output.ShouldBeEmpty();

        GitletCli.Run(_dir, "log").Output.ShouldContain("remove a\n");

        //最新的 commit 里已经没有 a.txt
        GitletCli.Run(_dir, "checkout", "--", "a.txt").Output.ShouldBe(ExceptionMessages.FileDoesNotExist + Environment.NewLine);
    }

    //commit 只推进当前分支
    [Fact]
    public void Commit_MovesOnlyTheCurrentBranch()
    {
        // Arrange
        GitletCli.Run(_dir, "init");

        GitletCli.Run(_dir, "branch", "dev");

        var logBefore = GitletCli.Run(_dir, "log").Output;

        File.WriteAllText(Path.Combine(_dir, "a.txt"), "x");
        GitletCli.Run(_dir, "add", "a.txt");

        // Act
        GitletCli.Run(_dir, "commit", "on main");

        // Assert
        GitletCli.Run(_dir, "log").Output.ShouldContain("on main\n");

        //dev 还停在原来的 commit
        GitletCli.Run(_dir, "checkout", "dev");
        GitletCli.Run(_dir, "log").Output.ShouldBe(logBefore);
    }

}
