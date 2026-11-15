using Shouldly;

namespace Gitlet.Tests.IntegrationTests;

public class CommandAddTestsIntegration : IDisposable
{

    private readonly string _dir = Path.Combine(Path.GetTempPath(), "gitlet-add-cli-" + Guid.NewGuid());

    //constructor
    public CommandAddTestsIntegration()
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
    [InlineData("add")]
    [InlineData("add", "a.txt", "b.txt")]
    [InlineData("add", "a.txt", "b.txt", "c.txt")]
    public void Add_WrongNumberOfArguments_IsIncorrectOperands(params string[] args)
    {
        // Arrange
        GitletCli.Run(_dir, "init");

        File.WriteAllText(Path.Combine(_dir, "a.txt"), "a");
        File.WriteAllText(Path.Combine(_dir, "b.txt"), "b");
        File.WriteAllText(Path.Combine(_dir, "c.txt"), "c");

        // Act
        var result = GitletCli.Run(_dir, args);

        // Assert
        result.Output.ShouldBe(ExceptionMessages.IncorrectOperands + Environment.NewLine);

        //什么都没暂存
        GitletCli.Run(_dir, "commit", "nothing").Output.ShouldBe(ExceptionMessages.NoChangesAdded + Environment.NewLine);
    }

    //还没 init 就 add
    [Fact]
    public void Add_BeforeInit_IsRejected()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_dir, "a.txt"), "a");

        // Act
        var result = GitletCli.Run(_dir, "add", "a.txt");

        // Assert
        result.Output.ShouldBe(ExceptionMessages.NotInitialized + Environment.NewLine);

        Directory.Exists(Path.Combine(_dir, ".gitlet")).ShouldBeFalse();
    }

    //文件不存在
    [Fact]
    public void Add_MissingFile_PrintsOnlyTheMessage()
    {
        // Arrange
        GitletCli.Run(_dir, "init");

        // Act
        var result = GitletCli.Run(_dir, "add", "nope.txt");

        // Assert
        result.Output.ShouldBe(ExceptionMessages.FileDoesNotExist + Environment.NewLine);
        result.Error.ShouldBeEmpty();
        result.ExitCode.ShouldBe(0);
    }

    //正常 add:什么都不打印,文件出现在 Staged Files 里
    [Theory]
    [InlineData("hello")]
    [InlineData("你好\nline 2\n")]
    [InlineData("")]
    public void Add_NewFile_PrintsNothingAndStagesIt(string content)
    {
        // Arrange
        GitletCli.Run(_dir, "init");

        File.WriteAllText(Path.Combine(_dir, "a.txt"), content);

        // Act
        var result = GitletCli.Run(_dir, "add", "a.txt");

        // Assert
        result.Output.ShouldBeEmpty();
        result.Error.ShouldBeEmpty();

        GitletCli.Run(_dir, "status").Output.ShouldContain("=== Staged Files ===\na.txt\n\n");

        //工作区的文件没被动过
        File.ReadAllText(Path.Combine(_dir, "a.txt")).ShouldBe(content);
    }

    //add 的是当时的内容:之后再改文件,commit 里还是 add 时的版本
    [Fact]
    public void Add_StagesTheContentAtThatMoment()
    {
        // Arrange
        GitletCli.Run(_dir, "init");

        File.WriteAllText(Path.Combine(_dir, "a.txt"), "staged");

        // Act
        GitletCli.Run(_dir, "add", "a.txt");

        //add 之后再改,不重新 add
        File.WriteAllText(Path.Combine(_dir, "a.txt"), "edited later");
        GitletCli.Run(_dir, "commit", "first");

        // Assert
        GitletCli.Run(_dir, "checkout", "--", "a.txt");
        File.ReadAllText(Path.Combine(_dir, "a.txt")).ShouldBe("staged");
    }

    //多个文件:按文件名排序显示
    [Fact]
    public void Add_SeveralFiles_ShowsThemSorted()
    {
        // Arrange
        GitletCli.Run(_dir, "init");

        File.WriteAllText(Path.Combine(_dir, "b.txt"), "b");
        File.WriteAllText(Path.Combine(_dir, "c.txt"), "c");
        File.WriteAllText(Path.Combine(_dir, "a.txt"), "a");

        // Act
        GitletCli.Run(_dir, "add", "b.txt");
        GitletCli.Run(_dir, "add", "c.txt");
        GitletCli.Run(_dir, "add", "a.txt");

        // Assert
        GitletCli.Run(_dir, "status").Output.ShouldContain("=== Staged Files ===\na.txt\nb.txt\nc.txt\n\n");
    }

    //和当前 commit 一样的文件:不需要暂存
    [Fact]
    public void Add_UnchangedSinceLastCommit_StagesNothing()
    {
        // Arrange
        GitletCli.Run(_dir, "init");

        File.WriteAllText(Path.Combine(_dir, "a.txt"), "v1");
        GitletCli.Run(_dir, "add", "a.txt");
        GitletCli.Run(_dir, "commit", "first");

        // Act
        var result = GitletCli.Run(_dir, "add", "a.txt");

        // Assert
        result.Output.ShouldBeEmpty();

        //暂存区是空的,没东西可提交
        GitletCli.Run(_dir, "commit", "again").Output.ShouldBe(ExceptionMessages.NoChangesAdded + Environment.NewLine);
    }

    //暂存了改动,又改回 commit 里的版本再 add:取消暂存
    [Fact]
    public void Add_RevertedToCommittedVersion_Unstages()
    {
        // Arrange
        GitletCli.Run(_dir, "init");

        File.WriteAllText(Path.Combine(_dir, "a.txt"), "v1");
        GitletCli.Run(_dir, "add", "a.txt");
        GitletCli.Run(_dir, "commit", "first");

        //改成 v2 并暂存
        File.WriteAllText(Path.Combine(_dir, "a.txt"), "v2");
        GitletCli.Run(_dir, "add", "a.txt");

        //再改回 v1
        File.WriteAllText(Path.Combine(_dir, "a.txt"), "v1");

        // Act
        GitletCli.Run(_dir, "add", "a.txt");

        // Assert
        GitletCli.Run(_dir, "commit", "again").Output.ShouldBe(ExceptionMessages.NoChangesAdded + Environment.NewLine);
    }

    //rm 之后再 add:取消删除
    [Theory]
    [InlineData("v1", false)] //内容和 commit 一样:只取消删除,没东西可提交
    [InlineData("v2", true)]  //内容变了:取消删除,并重新暂存
    public void Add_FileMarkedForRemoval_CancelsTheRemoval(string newContent, bool expectedStaged)
    {
        // Arrange
        GitletCli.Run(_dir, "init");

        File.WriteAllText(Path.Combine(_dir, "a.txt"), "v1");
        GitletCli.Run(_dir, "add", "a.txt");
        GitletCli.Run(_dir, "commit", "first");

        GitletCli.Run(_dir, "rm", "a.txt");

        File.WriteAllText(Path.Combine(_dir, "a.txt"), newContent);

        // Act
        GitletCli.Run(_dir, "add", "a.txt");

        // Assert
        var commit = GitletCli.Run(_dir, "commit", "after add");

        if (expectedStaged)
        {
            commit.Output.ShouldBeEmpty();
        }
        else
        {
            commit.Output.ShouldBe(ExceptionMessages.NoChangesAdded + Environment.NewLine);
        }

        //文件还在被跟踪:改坏再 checkout 能恢复
        File.WriteAllText(Path.Combine(_dir, "a.txt"), "broken");
        GitletCli.Run(_dir, "checkout", "--", "a.txt");
        File.ReadAllText(Path.Combine(_dir, "a.txt")).ShouldBe(newContent);
    }

}
