using System.Text.RegularExpressions;
using Shouldly;

namespace Gitlet.Tests.IntegrationTests;

public class CommandFindTestsIntegration : IDisposable
{

    private readonly string _dir = Path.Combine(Path.GetTempPath(), "gitlet-find-cli-" + Guid.NewGuid());

    //constructor
    public CommandFindTestsIntegration()
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
    [InlineData("find")]
    [InlineData("find", "initial", "commit")]
    public void Find_WrongNumberOfArguments_IsIncorrectOperands(params string[] args)
    {
        // Arrange
        GitletCli.Run(_dir, "init");

        // Act
        var result = GitletCli.Run(_dir, args);

        // Assert
        result.Output.ShouldBe(ExceptionMessages.IncorrectOperands + Environment.NewLine);
    }

    //还没 init 就 find
    [Fact]
    public void Find_BeforeInit_IsRejected()
    {
        // Act
        var result = GitletCli.Run(_dir, "find", "message");

        // Assert
        result.Output.ShouldBe(ExceptionMessages.NotInitialized + Environment.NewLine);

        Directory.Exists(Path.Combine(_dir, ".gitlet")).ShouldBeFalse();
    }

    //找到一个:打印的哈希和 log 里的一样
    [Theory]
    [InlineData("target")]
    [InlineData("a message with spaces")]
    [InlineData("initial commit")]
    public void Find_PrintsTheSameHashAsLog(string message)
    {
        // Arrange
        GitletCli.Run(_dir, "init");

        File.WriteAllText(Path.Combine(_dir, "a.txt"), "1");
        GitletCli.Run(_dir, "add", "a.txt");
        GitletCli.Run(_dir, "commit", "target");

        File.WriteAllText(Path.Combine(_dir, "a.txt"), "2");
        GitletCli.Run(_dir, "add", "a.txt");
        GitletCli.Run(_dir, "commit", "a message with spaces");

        //从 log 里取出这条消息对应的哈希
        var log = GitletCli.Run(_dir, "log").Output;
        var expected = Regex.Match(log, @"commit\s+([0-9a-f]{40})\nDate [^\n]+\n" + Regex.Escape(message) + @"\n").Groups[1].Value;

        // Act
        var result = GitletCli.Run(_dir, "find", message);

        // Assert
        result.Output.ShouldBe(expected + "\n");
        result.Error.ShouldBeEmpty();
        result.ExitCode.ShouldBe(0);
    }

    //同一个消息有多个 commit:每个一行
    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    public void Find_SameMessageOnSeveralCommits_PrintsAllOfThem(int count)
    {
        // Arrange
        GitletCli.Run(_dir, "init");

        for (var i = 0; i < count; i++)
        {
            File.WriteAllText(Path.Combine(_dir, "a.txt"), $"v{i}");
            GitletCli.Run(_dir, "add", "a.txt");
            GitletCli.Run(_dir, "commit", "same");
        }

        //log 里除了初始 commit 以外的哈希
        var expected = Regex.Matches(GitletCli.Run(_dir, "log").Output, @"commit\s+([0-9a-f]{40})").Select(m => m.Groups[1].Value).Take(count).ToList();

        // Act
        var result = GitletCli.Run(_dir, "find", "same");

        // Assert
        var lines = result.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        lines.ShouldBe(expected, ignoreOrder: true);
    }

    //其他分支上的 commit 也能找到
    [Fact]
    public void Find_FindsCommitsOnOtherBranches()
    {
        // Arrange
        GitletCli.Run(_dir, "init");

        GitletCli.Run(_dir, "branch", "dev");

        //dev
        GitletCli.Run(_dir, "checkout", "dev");
        File.WriteAllText(Path.Combine(_dir, "a.txt"), "x");
        GitletCli.Run(_dir, "add", "a.txt");
        GitletCli.Run(_dir, "commit", "only on dev");

        var expected = Regex.Match(GitletCli.Run(_dir, "log").Output, @"commit\s+([0-9a-f]{40})").Groups[1].Value;

        GitletCli.Run(_dir, "checkout", "main");

        // Act
        var result = GitletCli.Run(_dir, "find", "only on dev");

        // Assert
        result.Output.ShouldBe(expected + "\n");
    }

    //找不到:必须完全匹配
    [Theory]
    [InlineData("nothing like this")]
    [InlineData("initial")]        //只匹配一部分不算
    [InlineData("Initial Commit")] //区分大小写
    public void Find_NoExactMatch_PrintsOnlyTheMessage(string message)
    {
        // Arrange
        GitletCli.Run(_dir, "init");

        // Act
        var result = GitletCli.Run(_dir, "find", message);

        // Assert
        result.Output.ShouldBe(ExceptionMessages.NoCommitWithMessage + Environment.NewLine);
        result.Error.ShouldBeEmpty();
        result.ExitCode.ShouldBe(0);
    }

}
