using Shouldly;

namespace Gitlet.Tests.IntegrationTests;

public class CommandRmTestsIntegration : IDisposable
{

    private readonly string _dir = Path.Combine(Path.GetTempPath(), "gitlet-rm-cli-" + Guid.NewGuid());

    //constructor
    public CommandRmTestsIntegration()
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
    [InlineData("rm")]
    [InlineData("rm", "a.txt", "b.txt")]
    public void Rm_WrongNumberOfArguments_IsIncorrectOperands(params string[] args)
    {
        // Arrange
        GitletCli.Run(_dir, "init");

        File.WriteAllText(Path.Combine(_dir, "a.txt"), "a");
        File.WriteAllText(Path.Combine(_dir, "b.txt"), "b");
        GitletCli.Run(_dir, "add", "a.txt");
        GitletCli.Run(_dir, "add", "b.txt");
        GitletCli.Run(_dir, "commit", "base");

        // Act
        var result = GitletCli.Run(_dir, args);

        // Assert
        result.Output.ShouldBe(ExceptionMessages.IncorrectOperands + Environment.NewLine);

        //文件都还在
        File.Exists(Path.Combine(_dir, "a.txt")).ShouldBeTrue();
        File.Exists(Path.Combine(_dir, "b.txt")).ShouldBeTrue();
    }

    //还没 init 就 rm
    [Fact]
    public void Rm_BeforeInit_IsRejected()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_dir, "a.txt"), "a");

        // Act
        var result = GitletCli.Run(_dir, "rm", "a.txt");

        // Assert
        result.Output.ShouldBe(ExceptionMessages.NotInitialized + Environment.NewLine);

        File.Exists(Path.Combine(_dir, "a.txt")).ShouldBeTrue();
        Directory.Exists(Path.Combine(_dir, ".gitlet")).ShouldBeFalse();
    }

    //既没暂存也没跟踪:没有理由删
    [Theory]
    [InlineData(true)]  //工作区有这个文件
    [InlineData(false)] //文件根本不存在
    public void Rm_NeitherStagedNorTracked_PrintsOnlyTheMessage(bool fileExists)
    {
        // Arrange
        GitletCli.Run(_dir, "init");

        if (fileExists)
        {
            File.WriteAllText(Path.Combine(_dir, "a.txt"), "x");
        }

        // Act
        var result = GitletCli.Run(_dir, "rm", "a.txt");

        // Assert
        result.Output.ShouldBe(ExceptionMessages.NoReasonToRemove + Environment.NewLine);
        result.Error.ShouldBeEmpty();
        result.ExitCode.ShouldBe(0);

        //工作区的文件没被动过
        File.Exists(Path.Combine(_dir, "a.txt")).ShouldBe(fileExists);
    }

    //只暂存、还没跟踪:取消暂存,文件留在工作区
    [Fact]
    public void Rm_StagedButNotTracked_UnstagesAndKeepsFile()
    {
        // Arrange
        GitletCli.Run(_dir, "init");

        File.WriteAllText(Path.Combine(_dir, "a.txt"), "x");
        GitletCli.Run(_dir, "add", "a.txt");

        // Act
        var result = GitletCli.Run(_dir, "rm", "a.txt");

        // Assert
        result.Output.ShouldBeEmpty();

        File.ReadAllText(Path.Combine(_dir, "a.txt")).ShouldBe("x");

        //暂存区是空的,没东西可提交
        GitletCli.Run(_dir, "commit", "nothing").Output.ShouldBe(ExceptionMessages.NoChangesAdded + Environment.NewLine);
    }

    //已跟踪的文件:什么都不打印,暂存删除,并从工作区删掉
    [Fact]
    public void Rm_Tracked_PrintsNothingStagesRemovalAndDeletesFile()
    {
        // Arrange
        GitletCli.Run(_dir, "init");

        File.WriteAllText(Path.Combine(_dir, "a.txt"), "x");
        GitletCli.Run(_dir, "add", "a.txt");
        GitletCli.Run(_dir, "commit", "add a");

        // Act
        var result = GitletCli.Run(_dir, "rm", "a.txt");

        // Assert
        result.Output.ShouldBeEmpty();
        result.Error.ShouldBeEmpty();

        File.Exists(Path.Combine(_dir, "a.txt")).ShouldBeFalse();

        GitletCli.Run(_dir, "status").Output.ShouldContain("=== Removed Files ===\na.txt\n\n");
    }

    //已跟踪、但用户已经手动删掉了:仍然暂存删除
    [Fact]
    public void Rm_TrackedButAlreadyDeletedByUser_StillStagesRemoval()
    {
        // Arrange
        GitletCli.Run(_dir, "init");

        File.WriteAllText(Path.Combine(_dir, "a.txt"), "x");
        GitletCli.Run(_dir, "add", "a.txt");
        GitletCli.Run(_dir, "commit", "add a");

        File.Delete(Path.Combine(_dir, "a.txt"));

        // Act
        var result = GitletCli.Run(_dir, "rm", "a.txt");

        // Assert
        result.Output.ShouldBeEmpty();

        //删除可以被提交
        GitletCli.Run(_dir, "commit", "remove a").Output.ShouldBeEmpty();
        GitletCli.Run(_dir, "checkout", "--", "a.txt").Output.ShouldBe(ExceptionMessages.FileDoesNotExist + Environment.NewLine);
    }

    //已跟踪、又暂存了新的改动:取消暂存,并暂存删除
    [Fact]
    public void Rm_TrackedAndModifiedAndStaged_DropsTheStagedChange()
    {
        // Arrange
        GitletCli.Run(_dir, "init");

        File.WriteAllText(Path.Combine(_dir, "a.txt"), "v1");
        GitletCli.Run(_dir, "add", "a.txt");
        GitletCli.Run(_dir, "commit", "add a");

        File.WriteAllText(Path.Combine(_dir, "a.txt"), "v2");
        GitletCli.Run(_dir, "add", "a.txt");

        // Act
        GitletCli.Run(_dir, "rm", "a.txt");

        // Assert
        File.Exists(Path.Combine(_dir, "a.txt")).ShouldBeFalse();

        var status = GitletCli.Run(_dir, "status").Output;
        status.ShouldContain("=== Staged Files ===\n\n");
        status.ShouldContain("=== Removed Files ===\na.txt\n\n");
    }

    //rm 只影响指定的文件
    [Fact]
    public void Rm_DoesNotTouchOtherFiles()
    {
        // Arrange
        GitletCli.Run(_dir, "init");

        File.WriteAllText(Path.Combine(_dir, "a.txt"), "a");
        File.WriteAllText(Path.Combine(_dir, "b.txt"), "b");
        GitletCli.Run(_dir, "add", "a.txt");
        GitletCli.Run(_dir, "add", "b.txt");
        GitletCli.Run(_dir, "commit", "add a and b");

        // Act
        GitletCli.Run(_dir, "rm", "a.txt");
        GitletCli.Run(_dir, "commit", "remove a");

        // Assert
        File.Exists(Path.Combine(_dir, "a.txt")).ShouldBeFalse();
        File.ReadAllText(Path.Combine(_dir, "b.txt")).ShouldBe("b");

        //b.txt 还在最新的 commit 里
        File.WriteAllText(Path.Combine(_dir, "b.txt"), "broken");
        GitletCli.Run(_dir, "checkout", "--", "b.txt");
        File.ReadAllText(Path.Combine(_dir, "b.txt")).ShouldBe("b");
    }

}
