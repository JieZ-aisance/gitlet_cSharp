using Shouldly;

namespace Gitlet.Tests.IntegrationTests;

public class CommandInitTestsIntegration : IDisposable
{

    private readonly string _dir = Path.Combine(Path.GetTempPath(), "gitlet-init-cli-" + Guid.NewGuid());

    //constructor
    public CommandInitTestsIntegration()
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

    //参数个数不对:不应该建仓库
    [Theory]
    [InlineData("init", "extra")]
    [InlineData("init", "extra", "more")]
    public void Init_WrongNumberOfArguments_IsIncorrectOperands(params string[] args)
    {
        // Act
        var result = GitletCli.Run(_dir, args);

        // Assert
        result.Output.ShouldBe(ExceptionMessages.IncorrectOperands + Environment.NewLine);

        Directory.Exists(Path.Combine(_dir, ".gitlet")).ShouldBeFalse();
    }

    //正常 init:什么都不打印,建出仓库
    [Fact]
    public void Init_PrintsNothingAndCreatesRepository()
    {
        // Act
        var result = GitletCli.Run(_dir, "init");

        // Assert
        result.Output.ShouldBeEmpty();
        result.Error.ShouldBeEmpty();
        result.ExitCode.ShouldBe(0);

        Directory.Exists(Path.Combine(_dir, ".gitlet")).ShouldBeTrue();
    }

    //init 之后:只有一个初始 commit,时间是 1970 年
    [Fact]
    public void Init_CreatesInitialCommit()
    {
        // Arrange
        GitletCli.Run(_dir, "init");

        // Act
        var log = GitletCli.Run(_dir, "log").Output;

        // Assert
        log.ShouldStartWith("==============\ncommit  ");
        log.ShouldEndWith("\nDate Thu Jan 1 00:00:00 1970 +00:00\ninitial commit\n");

        //只有一条记录
        log.Split("==============\n", StringSplitOptions.RemoveEmptyEntries).Length.ShouldBe(1);
    }

    //init 之后工作区里已有的文件不受影响,也不会被自动跟踪
    [Fact]
    public void Init_LeavesExistingFilesAlone()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_dir, "a.txt"), "already here");

        // Act
        GitletCli.Run(_dir, "init");

        // Assert
        File.ReadAllText(Path.Combine(_dir, "a.txt")).ShouldBe("already here");

        //没被跟踪:rm 没有理由删它
        GitletCli.Run(_dir, "rm", "a.txt").Output.ShouldBe(ExceptionMessages.NoReasonToRemove + Environment.NewLine);
    }

    //init 两次:第二次只打印错误消息,原来的仓库不变
    [Fact]
    public void Init_Twice_PrintsOnlyTheMessageAndKeepsRepository()
    {
        // Arrange
        GitletCli.Run(_dir, "init");

        File.WriteAllText(Path.Combine(_dir, "a.txt"), "x");
        GitletCli.Run(_dir, "add", "a.txt");
        GitletCli.Run(_dir, "commit", "first");

        var logBefore = GitletCli.Run(_dir, "log").Output;

        // Act
        var result = GitletCli.Run(_dir, "init");

        // Assert
        result.Output.ShouldBe(ExceptionMessages.AlreadyInitialized + Environment.NewLine);
        result.Error.ShouldBeEmpty();
        result.ExitCode.ShouldBe(0);

        GitletCli.Run(_dir, "log").Output.ShouldBe(logBefore);
    }

    //初始 commit 在每个仓库里都一样
    [Fact]
    public void Init_InitialCommitIsIdenticalInEveryRepository()
    {
        // Arrange
        var otherDir = Path.Combine(Path.GetTempPath(), "gitlet-init-cli-" + Guid.NewGuid());

        Directory.CreateDirectory(otherDir);

        try
        {
            // Act
            GitletCli.Run(_dir, "init");
            GitletCli.Run(otherDir, "init");

            // Assert
            GitletCli.Run(otherDir, "log").Output.ShouldBe(GitletCli.Run(_dir, "log").Output);
        }
        finally
        {
            Directory.Delete(otherDir, recursive: true);
        }
    }

}
