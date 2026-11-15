using System.Text.RegularExpressions;
using Shouldly;

namespace Gitlet.Tests.IntegrationTests;

public class CommandLogTestsIntegration : IDisposable
{

    private readonly string _dir = Path.Combine(Path.GetTempPath(), "gitlet-log-cli-" + Guid.NewGuid());

    //constructor
    public CommandLogTestsIntegration()
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
    [InlineData("log", "extra")]
    [InlineData("log", "extra", "more")]
    public void Log_WrongNumberOfArguments_IsIncorrectOperands(params string[] args)
    {
        // Arrange
        GitletCli.Run(_dir, "init");

        // Act
        var result = GitletCli.Run(_dir, args);

        // Assert
        result.Output.ShouldBe(ExceptionMessages.IncorrectOperands + Environment.NewLine);
    }

    //还没 init 就 log
    [Fact]
    public void Log_BeforeInit_IsRejected()
    {
        // Act
        var result = GitletCli.Run(_dir, "log");

        // Assert
        result.Output.ShouldBe(ExceptionMessages.NotInitialized + Environment.NewLine);

        Directory.Exists(Path.Combine(_dir, ".gitlet")).ShouldBeFalse();
    }

    //刚 init 的仓库:只有初始 commit,格式固定
    [Fact]
    public void Log_FreshRepository_PrintsExactLayoutToStdout()
    {
        // Arrange
        GitletCli.Run(_dir, "init");

        // Act
        var result = GitletCli.Run(_dir, "log");

        // Assert
        result.Error.ShouldBeEmpty();
        result.ExitCode.ShouldBe(0);

        //GitletCli 把进程时区设成了 UTC
        result.Output.ShouldMatch(
            @"^==============\ncommit  [0-9a-f]{40}\nDate Thu Jan 1 00:00:00 1970 \+00:00\ninitial commit\n$");
    }

    //每个 commit 一条,最新的在最上面
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(5)]
    public void Log_HasOneEntryPerCommit_NewestFirst(int count)
    {
        // Arrange
        GitletCli.Run(_dir, "init");

        for (var i = 1; i <= count; i++)
        {
            File.WriteAllText(Path.Combine(_dir, "a.txt"), $"v{i}");
            GitletCli.Run(_dir, "add", "a.txt");
            GitletCli.Run(_dir, "commit", $"message {i}");
        }

        // Act
        var log = GitletCli.Run(_dir, "log").Output;

        // Assert
        var messages = Regex.Matches(log, @"Date [^\n]+\n([^\n]*)\n").Select(m => m.Groups[1].Value).ToList();

        messages.Count.ShouldBe(count + 1);

        //从新到旧:message N ... message 1, initial commit
        for (var i = 0; i < count; i++)
        {
            messages[i].ShouldBe($"message {count - i}");
        }
        messages[count].ShouldBe("initial commit");

        //哈希各不相同
        Regex.Matches(log, @"commit\s+([0-9a-f]{40})").Select(m => m.Groups[1].Value).Distinct().Count().ShouldBe(count + 1);
    }

    //log 不改变任何东西:连续两次输出一样
    [Fact]
    public void Log_Twice_PrintsTheSame()
    {
        // Arrange
        GitletCli.Run(_dir, "init");

        File.WriteAllText(Path.Combine(_dir, "a.txt"), "x");
        GitletCli.Run(_dir, "add", "a.txt");
        GitletCli.Run(_dir, "commit", "first");

        // Act
        var first = GitletCli.Run(_dir, "log").Output;
        var second = GitletCli.Run(_dir, "log").Output;

        // Assert
        second.ShouldBe(first);
    }

    //log 只显示当前分支的历史
    [Fact]
    public void Log_ShowsOnlyTheHistoryOfTheCurrentBranch()
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

        // Act
        var devLog = GitletCli.Run(_dir, "log").Output;

        // Assert
        devLog.ShouldContain("only on dev\n");
        devLog.ShouldContain("base\n");

        //main
        GitletCli.Run(_dir, "checkout", "main");
        File.WriteAllText(Path.Combine(_dir, "main.txt"), "main");
        GitletCli.Run(_dir, "add", "main.txt");
        GitletCli.Run(_dir, "commit", "only on main");

        // Act
        var mainLog = GitletCli.Run(_dir, "log").Output;

        // Assert
        mainLog.ShouldContain("only on main\n");
        mainLog.ShouldContain("base\n");
        mainLog.ShouldNotContain("only on dev");
    }

    //reset 之后,后面的 commit 不再出现在 log 里
    [Fact]
    public void Log_AfterReset_StopsAtThatCommit()
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

        GitletCli.Run(_dir, "reset", firstId);

        // Act
        var log = GitletCli.Run(_dir, "log").Output;

        // Assert
        log.ShouldBe(logAtFirst);
    }

}
