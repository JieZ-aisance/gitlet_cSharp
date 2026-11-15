using Shouldly;

namespace Gitlet.Tests.IntegrationTests;

public class ProgramTestsIntegration : IDisposable
{

    private readonly string _dir = Path.Combine(Path.GetTempPath(), "gitlet-program-cli-" + Guid.NewGuid());

    //constructor
    public ProgramTestsIntegration()
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

    //不带任何参数:init 之前和之后都一样
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NoArguments_AsksForACommand(bool initialized)
    {
        // Arrange
        if (initialized)
        {
            GitletCli.Run(_dir, "init");
        }

        // Act
        var result = GitletCli.Run(_dir);

        // Assert
        result.Output.ShouldBe(ExceptionMessages.PleaseEnterCommand + Environment.NewLine);
        result.Error.ShouldBeEmpty();
        result.ExitCode.ShouldBe(0);
    }

    //不认识的命令
    [Theory]
    [InlineData("frobnicate")]
    [InlineData("Init")]              //命令区分大小写
    [InlineData("checkout-branch")]
    [InlineData("frobnicate", "a", "b")]
    public void UnknownCommand_IsRejected(params string[] args)
    {
        // Arrange
        GitletCli.Run(_dir, "init");

        // Act
        var result = GitletCli.Run(_dir, args);

        // Assert
        result.Output.ShouldBe(ExceptionMessages.NoSuchCommand + Environment.NewLine);
        result.Error.ShouldBeEmpty();
        result.ExitCode.ShouldBe(0);
    }

    //还没 init:先报"未初始化",而不是"没有这个命令"
    [Fact]
    public void UnknownCommand_BeforeInit_IsNotInitialized()
    {
        // Act
        var result = GitletCli.Run(_dir, "frobnicate");

        // Assert
        result.Output.ShouldBe(ExceptionMessages.NotInitialized + Environment.NewLine);

        Directory.Exists(Path.Combine(_dir, ".gitlet")).ShouldBeFalse();
    }

    //两个目录各自是独立的仓库
    [Fact]
    public void Scenario_TwoRepositoriesAreIndependent()
    {
        // Arrange
        var otherDir = Path.Combine(Path.GetTempPath(), "gitlet-program-cli-" + Guid.NewGuid());

        Directory.CreateDirectory(otherDir);

        try
        {
            GitletCli.Run(_dir, "init");
            GitletCli.Run(otherDir, "init");

            //只在第一个仓库里提交
            File.WriteAllText(Path.Combine(_dir, "a.txt"), "x");
            GitletCli.Run(_dir, "add", "a.txt");
            GitletCli.Run(_dir, "commit", "only here");

            // Act
            var log = GitletCli.Run(_dir, "log").Output;
            var otherLog = GitletCli.Run(otherDir, "log").Output;

            // Assert
            log.ShouldContain("only here");
            otherLog.ShouldNotContain("only here");

            File.Exists(Path.Combine(otherDir, "a.txt")).ShouldBeFalse();
        }
        finally
        {
            Directory.Delete(otherDir, recursive: true);
        }
    }

}
