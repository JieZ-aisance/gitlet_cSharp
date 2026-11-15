using Shouldly;

namespace Gitlet.Tests.IntegrationTests;

public class CommandBranchTestsIntegration : IDisposable
{

    private readonly string _dir = Path.Combine(Path.GetTempPath(), "gitlet-branch-cli-" + Guid.NewGuid());

    //constructor
    public CommandBranchTestsIntegration()
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
    [InlineData("branch")]
    [InlineData("branch", "dev", "extra")]
    public void Branch_WrongNumberOfArguments_IsIncorrectOperands(params string[] args)
    {
        // Arrange
        GitletCli.Run(_dir, "init");

        // Act
        var result = GitletCli.Run(_dir, args);

        // Assert
        result.Output.ShouldBe(ExceptionMessages.IncorrectOperands + Environment.NewLine);

        //没有建出分支
        GitletCli.Run(_dir, "checkout", "dev").Output.ShouldBe(ExceptionMessages.BranchNoExists + Environment.NewLine);
    }

    //还没 init 就 branch
    [Fact]
    public void Branch_BeforeInit_IsRejected()
    {
        // Act
        var result = GitletCli.Run(_dir, "branch", "dev");

        // Assert
        result.Output.ShouldBe(ExceptionMessages.NotInitialized + Environment.NewLine);

        Directory.Exists(Path.Combine(_dir, ".gitlet")).ShouldBeFalse();
    }

    //正常建分支:什么都不打印,不切换过去
    [Theory]
    [InlineData("dev")]
    [InlineData("feature")]
    [InlineData("fix-1")]
    public void Branch_PrintsNothingAndDoesNotSwitch(string name)
    {
        // Arrange
        GitletCli.Run(_dir, "init");

        // Act
        var result = GitletCli.Run(_dir, "branch", name);

        // Assert
        result.Output.ShouldBeEmpty();
        result.Error.ShouldBeEmpty();
        result.ExitCode.ShouldBe(0);

        //当前分支还是 main
        GitletCli.Run(_dir, "checkout", "main").Output.ShouldBe(ExceptionMessages.ALreadyOnTheBranch + Environment.NewLine);

        var status = GitletCli.Run(_dir, "status").Output;
        status.ShouldContain("*main\n");
        status.ShouldContain($"{name}\n");
        status.ShouldNotContain($"*{name}");
    }

    //新分支指向建分支那一刻的 commit
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(3)]
    public void Branch_PointsAtTheCommitWhereItWasCreated(int commitsBefore)
    {
        // Arrange
        GitletCli.Run(_dir, "init");

        for (var i = 0; i < commitsBefore; i++)
        {
            File.WriteAllText(Path.Combine(_dir, "a.txt"), $"v{i}");
            GitletCli.Run(_dir, "add", "a.txt");
            GitletCli.Run(_dir, "commit", $"c{i}");
        }

        var logAtCreation = GitletCli.Run(_dir, "log").Output;

        // Act
        GitletCli.Run(_dir, "branch", "dev");

        //main 之后继续往前走
        File.WriteAllText(Path.Combine(_dir, "later.txt"), "later");
        GitletCli.Run(_dir, "add", "later.txt");
        GitletCli.Run(_dir, "commit", "later on main");

        // Assert
        GitletCli.Run(_dir, "checkout", "dev");
        GitletCli.Run(_dir, "log").Output.ShouldBe(logAtCreation);

        File.Exists(Path.Combine(_dir, "later.txt")).ShouldBeFalse();
    }

    //同名分支已经存在
    [Theory]
    [InlineData("main")]
    [InlineData("dev")]
    public void Branch_ExistingName_PrintsOnlyTheMessage(string name)
    {
        // Arrange
        GitletCli.Run(_dir, "init");

        GitletCli.Run(_dir, "branch", "dev");

        // Act
        var result = GitletCli.Run(_dir, "branch", name);

        // Assert
        result.Output.ShouldBe(ExceptionMessages.BranchAlreadyExists + Environment.NewLine);
        result.Error.ShouldBeEmpty();
        result.ExitCode.ShouldBe(0);
    }

}
