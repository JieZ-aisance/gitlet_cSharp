using System.Text.RegularExpressions;
using Shouldly;
using Xunit;

namespace Gitlet.Tests.IntegrationTests;

// 这里测的是整个程序:命令行参数进去,控制台输出和磁盘文件出来。
// 每条命令的细节逻辑已经在 UnitTests/RepositoryTests 里测过,这里只关心
// Program.cs 这一层(参数校验、命令分派、输出)和几个完整流程。
public class RepositoryIntegrationTests : IDisposable
{
    private readonly string _dir =
        Path.Combine(Path.GetTempPath(), "gitlet-cli-" + Guid.NewGuid());

    public RepositoryIntegrationTests()
    {
        Directory.CreateDirectory(_dir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_dir))
        {
            Directory.Delete(_dir, recursive: true);
        }
    }

    // ---------- 辅助方法 ----------

    private CliResult Gitlet(params string[] args) => GitletCli.Run(_dir, args);

    private string PathOf(string name) => Path.Combine(_dir, name);

    private void WriteFile(string name, string content) => File.WriteAllText(PathOf(name), content);

    private string ReadFile(string name) => File.ReadAllText(PathOf(name));

    private void Init() => Gitlet("init");

    private void AddAndCommit(string name, string content, string message)
    {
        WriteFile(name, content);
        Gitlet("add", name);
        Gitlet("commit", message);
    }

    private static List<string> HashesIn(string log) =>
        Regex.Matches(log, @"commit\s+([0-9a-f]{40})").Select(m => m.Groups[1].Value).ToList();

    private string HeadHash() => HashesIn(Gitlet("log").Output)[0];

    // ====================================================================
    // 参数校验
    // ====================================================================

    [Fact]
    public void NoArguments_AsksForACommand()
    {
        Gitlet().Output.ShouldBe(ExceptionMessages.PleaseEnterCommand + Environment.NewLine);
    }

    [Fact]
    public void UnknownCommand_IsRejected()
    {
        Init();

        Gitlet("frobnicate").Output.ShouldBe(ExceptionMessages.NoSuchCommand + Environment.NewLine);
    }

    [Theory]
    [InlineData("add", "a.txt")]
    [InlineData("commit", "message")]
    [InlineData("rm", "a.txt")]
    [InlineData("log")]
    [InlineData("global-log")]
    [InlineData("find", "message")]
    [InlineData("branch", "dev")]
    [InlineData("rm-branch", "dev")]
    [InlineData("status")]
    [InlineData("checkout", "dev")]
    [InlineData("reset", "abc")]
    public void CommandsBeforeInit_AreRejected(params string[] args)
    {
        Gitlet(args).Output.ShouldBe(ExceptionMessages.NotInitialized + Environment.NewLine);
        Directory.Exists(PathOf(".gitlet")).ShouldBeFalse(); // 没有被悄悄创建
    }

    [Theory]
    [InlineData("init", "extra")]
    [InlineData("add")]
    [InlineData("add", "a.txt", "b.txt")]
    [InlineData("commit")]
    [InlineData("commit", "one", "two")]
    [InlineData("rm")]
    [InlineData("log", "extra")]
    [InlineData("global-log", "extra")]
    [InlineData("find")]
    [InlineData("branch")]
    [InlineData("rm-branch")]
    [InlineData("status", "extra")]
    [InlineData("reset")]
    [InlineData("checkout")]
    [InlineData("checkout", "a", "b")]
    [InlineData("checkout", "a", "b", "c")]
    [InlineData("checkout", "a", "b", "c", "d")]
    public void WrongNumberOfArguments_IsIncorrectOperands(params string[] args)
    {
        Init();

        Gitlet(args).Output.ShouldBe(ExceptionMessages.IncorrectOperands + Environment.NewLine);
    }

    // ====================================================================
    // 输出方式
    // ====================================================================

    [Fact]
    public void Init_PrintsNothing_AndCreatesRepository()
    {
        var result = Gitlet("init");

        result.Output.ShouldBeEmpty();
        result.Error.ShouldBeEmpty();
        Directory.Exists(PathOf(".gitlet")).ShouldBeTrue();
    }

    [Fact]
    public void Errors_PrintOnlyTheMessage_NoStackTrace()
    {
        Init();

        var result = Gitlet("init");

        result.Output.ShouldBe(ExceptionMessages.AlreadyInitialized + Environment.NewLine);
        result.Error.ShouldBeEmpty();
    }

    [Fact]
    public void Status_OnFreshRepository_PrintsToStdout()
    {
        Init();

        Gitlet("status").Output.ShouldBe(
            "=== Branches ===\n*main\n\n" +
            "=== Staged Files ===\n\n" +
            "=== Removed Files ===\n\n" +
            "=== Modifications Not Staged For Commit ===\n\n" +
            "=== Untracked Files ===\n\n");
    }

    [Fact]
    public void Log_ShowsDateInTheProcessTimeZone()
    {
        Init();

        // TZ=UTC,所以 1970 年的初始 commit 显示为 UTC 零点
        Gitlet("log").Output.ShouldContain("Thu Jan 1 00:00:00 1970 +00:00");
    }

    [Fact]
    public void Find_PrintsTheSameHashAsLog()
    {
        Init();
        AddAndCommit("a.txt", "x", "target");

        Gitlet("find", "target").Output.ShouldBe(HeadHash() + "\n");
    }

    // ====================================================================
    // checkout 的三种写法如何分派
    // ====================================================================

    [Fact]
    public void Checkout_DashDashFile_RestoresFileFromHead()
    {
        Init();
        AddAndCommit("a.txt", "committed", "first");
        WriteFile("a.txt", "scribbled");

        Gitlet("checkout", "--", "a.txt");

        ReadFile("a.txt").ShouldBe("committed");
    }

    [Fact]
    public void Checkout_CommitIdDashDashFile_RestoresFileFromThatCommit()
    {
        Init();
        AddAndCommit("a.txt", "v1", "first");
        var firstId = HeadHash();
        AddAndCommit("a.txt", "v2", "second");

        Gitlet("checkout", firstId[..8], "--", "a.txt");

        ReadFile("a.txt").ShouldBe("v1");
    }

    [Fact]
    public void Checkout_BranchName_SwitchesBranch()
    {
        Init();
        Gitlet("branch", "dev");

        Gitlet("checkout", "dev");

        Gitlet("status").Output.ShouldContain("*dev\n");
    }

    [Fact]
    public void Checkout_UnknownBranch_PrintsError()
    {
        Init();

        Gitlet("checkout", "ghost").Output.ShouldBe(ExceptionMessages.BranchNoExists + Environment.NewLine);
    }

    // ====================================================================
    // 完整流程
    // ====================================================================

    [Fact]
    public void Scenario_AddCommitLog()
    {
        Init();
        WriteFile("hello.txt", "hello");

        Gitlet("add", "hello.txt").Output.ShouldBeEmpty();
        Gitlet("status").Output.ShouldContain("=== Staged Files ===\nhello.txt\n");
        Gitlet("commit", "add hello").Output.ShouldBeEmpty();
        Gitlet("status").Output.ShouldContain("=== Staged Files ===\n\n");

        var log = Gitlet("log").Output;
        HashesIn(log).Count.ShouldBe(2);
        log.IndexOf("add hello", StringComparison.Ordinal)
            .ShouldBeLessThan(log.IndexOf("initial commit", StringComparison.Ordinal));
    }

    [Fact]
    public void Scenario_RemoveTrackedFileAndCommit()
    {
        Init();
        AddAndCommit("a.txt", "x", "add a");

        Gitlet("rm", "a.txt");

        File.Exists(PathOf("a.txt")).ShouldBeFalse();
        Gitlet("status").Output.ShouldContain("=== Removed Files ===\na.txt\n");

        Gitlet("commit", "remove a");

        Gitlet("checkout", "--", "a.txt").Output
            .ShouldBe(ExceptionMessages.FileDoesNotExist + Environment.NewLine);
    }

    [Fact]
    public void Scenario_BranchesDivergeAndSwitchBack()
    {
        Init();
        AddAndCommit("shared.txt", "base", "base");
        Gitlet("branch", "dev");
        Gitlet("checkout", "dev");
        AddAndCommit("shared.txt", "dev version", "dev edit");
        AddAndCommit("dev-only.txt", "d", "dev file");

        Gitlet("checkout", "main");

        ReadFile("shared.txt").ShouldBe("base");
        File.Exists(PathOf("dev-only.txt")).ShouldBeFalse();
        Gitlet("log").Output.ShouldNotContain("dev edit");
        Gitlet("global-log").Output.ShouldContain("dev edit");

        Gitlet("checkout", "dev");

        ReadFile("shared.txt").ShouldBe("dev version");
        ReadFile("dev-only.txt").ShouldBe("d");
    }

    [Fact]
    public void Scenario_ResetToEarlierCommit()
    {
        Init();
        AddAndCommit("a.txt", "v1", "first");
        var firstId = HeadHash();
        AddAndCommit("a.txt", "v2", "second");
        AddAndCommit("b.txt", "b", "third");

        Gitlet("reset", firstId);

        ReadFile("a.txt").ShouldBe("v1");
        File.Exists(PathOf("b.txt")).ShouldBeFalse();
        HashesIn(Gitlet("log").Output).Count.ShouldBe(2);
    }

    [Fact]
    public void Scenario_TwoRepositoriesAreIndependent()
    {
        Init();
        AddAndCommit("a.txt", "x", "only here");
        var otherDir = Path.Combine(Path.GetTempPath(), "gitlet-cli-" + Guid.NewGuid());
        try
        {
            Directory.CreateDirectory(otherDir);
            GitletCli.Run(otherDir, "init");

            GitletCli.Run(otherDir, "log").Output.ShouldNotContain("only here");
        }
        finally
        {
            Directory.Delete(otherDir, recursive: true);
        }
    }
}
