using System.Text.RegularExpressions;
using Shouldly;

namespace Gitlet.Tests.IntegrationTests;

public class CommandGlobalLogTestsIntegration : IDisposable
{

    private readonly string _dir = Path.Combine(Path.GetTempPath(), "gitlet-globallog-cli-" + Guid.NewGuid());

    //constructor
    public CommandGlobalLogTestsIntegration()
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
    [InlineData("global-log", "extra")]
    [InlineData("global-log", "extra", "more")]
    public void GlobalLog_WrongNumberOfArguments_IsIncorrectOperands(params string[] args)
    {
        // Arrange
        GitletCli.Run(_dir, "init");

        // Act
        var result = GitletCli.Run(_dir, args);

        // Assert
        result.Output.ShouldBe(ExceptionMessages.IncorrectOperands + Environment.NewLine);
    }

    //还没 init 就 global-log
    [Fact]
    public void GlobalLog_BeforeInit_IsRejected()
    {
        // Act
        var result = GitletCli.Run(_dir, "global-log");

        // Assert
        result.Output.ShouldBe(ExceptionMessages.NotInitialized + Environment.NewLine);

        Directory.Exists(Path.Combine(_dir, ".gitlet")).ShouldBeFalse();
    }

    //刚 init 的仓库:和 log 的输出完全一样
    [Fact]
    public void GlobalLog_FreshRepository_PrintsTheSameAsLog()
    {
        // Arrange
        GitletCli.Run(_dir, "init");

        // Act
        var result = GitletCli.Run(_dir, "global-log");

        // Assert
        result.Error.ShouldBeEmpty();
        result.ExitCode.ShouldBe(0);

        result.Output.ShouldBe(GitletCli.Run(_dir, "log").Output);
    }

    //每个 commit 一条(顺序不保证)
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(3)]
    public void GlobalLog_HasOneEntryPerCommit(int extraCommits)
    {
        // Arrange
        GitletCli.Run(_dir, "init");

        for (var i = 0; i < extraCommits; i++)
        {
            File.WriteAllText(Path.Combine(_dir, "a.txt"), $"v{i}");
            GitletCli.Run(_dir, "add", "a.txt");
            GitletCli.Run(_dir, "commit", $"c{i}");
        }

        // Act
        var globalLog = GitletCli.Run(_dir, "global-log").Output;

        // Assert
        var hashes = Regex.Matches(globalLog, @"commit\s+([0-9a-f]{40})").Select(m => m.Groups[1].Value).ToList();

        //单分支时,和 log 里的 commit 是同一批
        var logHashes = Regex.Matches(GitletCli.Run(_dir, "log").Output, @"commit\s+([0-9a-f]{40})").Select(m => m.Groups[1].Value).ToList();

        hashes.Count.ShouldBe(extraCommits + 1);
        hashes.ShouldBe(logHashes, ignoreOrder: true);
    }

    //其他分支上的 commit 也会显示,log 不会
    [Fact]
    public void GlobalLog_IncludesCommitsFromOtherBranches()
    {
        // Arrange
        GitletCli.Run(_dir, "init");

        GitletCli.Run(_dir, "branch", "dev");

        //dev
        GitletCli.Run(_dir, "checkout", "dev");
        File.WriteAllText(Path.Combine(_dir, "a.txt"), "x");
        GitletCli.Run(_dir, "add", "a.txt");
        GitletCli.Run(_dir, "commit", "only on dev");

        GitletCli.Run(_dir, "checkout", "main");

        // Act
        var globalLog = GitletCli.Run(_dir, "global-log").Output;

        // Assert
        globalLog.ShouldContain("only on dev\n");
        globalLog.ShouldContain("initial commit\n");

        GitletCli.Run(_dir, "log").Output.ShouldNotContain("only on dev");
    }

    //分支被删掉、或者被 reset 甩掉的 commit,仍然在 global-log 里
    [Theory]
    [InlineData(true)]  //rm-branch
    [InlineData(false)] //reset
    public void GlobalLog_IncludesCommitsNoLongerOnAnyBranch(bool viaRmBranch)
    {
        // Arrange
        GitletCli.Run(_dir, "init");

        var initialId = Regex.Match(GitletCli.Run(_dir, "log").Output, @"commit\s+([0-9a-f]{40})").Groups[1].Value;

        if (viaRmBranch)
        {
            GitletCli.Run(_dir, "branch", "dev");
            GitletCli.Run(_dir, "checkout", "dev");
        }

        File.WriteAllText(Path.Combine(_dir, "a.txt"), "x");
        GitletCli.Run(_dir, "add", "a.txt");
        GitletCli.Run(_dir, "commit", "orphan");

        if (viaRmBranch)
        {
            GitletCli.Run(_dir, "checkout", "main");
            GitletCli.Run(_dir, "rm-branch", "dev");
        }
        else
        {
            GitletCli.Run(_dir, "reset", initialId);
        }

        // Act
        var globalLog = GitletCli.Run(_dir, "global-log").Output;

        // Assert
        globalLog.ShouldContain("orphan\n");

        GitletCli.Run(_dir, "log").Output.ShouldNotContain("orphan");
    }

}
