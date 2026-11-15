using Shouldly;

namespace Gitlet.Tests.IntegrationTests;

public class CommandCheckoutFileTestsIntegration : IDisposable
{

    private readonly string _dir = Path.Combine(Path.GetTempPath(), "gitlet-checkoutfile-cli-" + Guid.NewGuid());

    //constructor
    public CommandCheckoutFileTestsIntegration()
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

    //checkout 的参数形状不对
    [Theory]
    [InlineData("checkout")]
    [InlineData("checkout", "a.txt", "--")]             //-- 的位置反了
    [InlineData("checkout", "++", "a.txt")]             //分隔符不是 --
    [InlineData("checkout", "--", "a.txt", "b.txt")]
    [InlineData("checkout", "--", "a.txt", "b.txt", "c.txt")]
    public void Checkout_WrongOperands_IsIncorrectOperands(params string[] args)
    {
        // Arrange
        GitletCli.Run(_dir, "init");

        File.WriteAllText(Path.Combine(_dir, "a.txt"), "committed");
        GitletCli.Run(_dir, "add", "a.txt");
        GitletCli.Run(_dir, "commit", "first");

        File.WriteAllText(Path.Combine(_dir, "a.txt"), "scribbled");

        // Act
        var result = GitletCli.Run(_dir, args);

        // Assert
        result.Output.ShouldBe(ExceptionMessages.IncorrectOperands + Environment.NewLine);

        //文件没被恢复
        File.ReadAllText(Path.Combine(_dir, "a.txt")).ShouldBe("scribbled");
    }

    //还没 init 就 checkout
    [Fact]
    public void CheckoutFile_BeforeInit_IsRejected()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_dir, "a.txt"), "x");

        // Act
        var result = GitletCli.Run(_dir, "checkout", "--", "a.txt");

        // Assert
        result.Output.ShouldBe(ExceptionMessages.NotInitialized + Environment.NewLine);

        File.ReadAllText(Path.Combine(_dir, "a.txt")).ShouldBe("x");
        Directory.Exists(Path.Combine(_dir, ".gitlet")).ShouldBeFalse();
    }

    //改坏的文件:什么都不打印,恢复成 HEAD 里的版本
    [Theory]
    [InlineData("committed")]
    [InlineData("你好\nline 2\n")]
    [InlineData("")]
    public void CheckoutFile_ModifiedFile_PrintsNothingAndRestoresIt(string committedContent)
    {
        // Arrange
        GitletCli.Run(_dir, "init");

        File.WriteAllText(Path.Combine(_dir, "a.txt"), committedContent);
        GitletCli.Run(_dir, "add", "a.txt");
        GitletCli.Run(_dir, "commit", "first");

        File.WriteAllText(Path.Combine(_dir, "a.txt"), "scribbled");

        // Act
        var result = GitletCli.Run(_dir, "checkout", "--", "a.txt");

        // Assert
        result.Output.ShouldBeEmpty();
        result.Error.ShouldBeEmpty();
        result.ExitCode.ShouldBe(0);

        File.ReadAllText(Path.Combine(_dir, "a.txt")).ShouldBe(committedContent);
    }

    //被删掉的文件:重新建出来
    [Fact]
    public void CheckoutFile_DeletedFile_RecreatesIt()
    {
        // Arrange
        GitletCli.Run(_dir, "init");

        File.WriteAllText(Path.Combine(_dir, "a.txt"), "committed");
        GitletCli.Run(_dir, "add", "a.txt");
        GitletCli.Run(_dir, "commit", "first");

        File.Delete(Path.Combine(_dir, "a.txt"));

        // Act
        GitletCli.Run(_dir, "checkout", "--", "a.txt");

        // Assert
        File.ReadAllText(Path.Combine(_dir, "a.txt")).ShouldBe("committed");
    }

    //恢复的是最新 commit 里的版本,不是更早的
    [Fact]
    public void CheckoutFile_UsesTheHeadCommit()
    {
        // Arrange
        GitletCli.Run(_dir, "init");

        File.WriteAllText(Path.Combine(_dir, "a.txt"), "v1");
        GitletCli.Run(_dir, "add", "a.txt");
        GitletCli.Run(_dir, "commit", "first");

        File.WriteAllText(Path.Combine(_dir, "a.txt"), "v2");
        GitletCli.Run(_dir, "add", "a.txt");
        GitletCli.Run(_dir, "commit", "second");

        File.WriteAllText(Path.Combine(_dir, "a.txt"), "scribbled");

        // Act
        GitletCli.Run(_dir, "checkout", "--", "a.txt");

        // Assert
        File.ReadAllText(Path.Combine(_dir, "a.txt")).ShouldBe("v2");
    }

    //只恢复指定的文件,不动暂存区,也不动别的文件
    [Fact]
    public void CheckoutFile_DoesNotStageOrTouchOtherFiles()
    {
        // Arrange
        GitletCli.Run(_dir, "init");

        File.WriteAllText(Path.Combine(_dir, "a.txt"), "a committed");
        File.WriteAllText(Path.Combine(_dir, "b.txt"), "b committed");
        GitletCli.Run(_dir, "add", "a.txt");
        GitletCli.Run(_dir, "add", "b.txt");
        GitletCli.Run(_dir, "commit", "first");

        File.WriteAllText(Path.Combine(_dir, "a.txt"), "a scribbled");
        File.WriteAllText(Path.Combine(_dir, "b.txt"), "b scribbled");

        // Act
        GitletCli.Run(_dir, "checkout", "--", "a.txt");

        // Assert
        File.ReadAllText(Path.Combine(_dir, "a.txt")).ShouldBe("a committed");
        File.ReadAllText(Path.Combine(_dir, "b.txt")).ShouldBe("b scribbled");

        //暂存区是空的,没东西可提交
        GitletCli.Run(_dir, "commit", "nothing").Output.ShouldBe(ExceptionMessages.NoChangesAdded + Environment.NewLine);
    }

    //文件不在 HEAD 里
    [Theory]
    [InlineData(false)] //文件完全不存在
    [InlineData(true)]  //文件只是暂存了,还没进 commit
    public void CheckoutFile_NotInHeadCommit_PrintsOnlyTheMessage(bool staged)
    {
        // Arrange
        GitletCli.Run(_dir, "init");

        if (staged)
        {
            File.WriteAllText(Path.Combine(_dir, "a.txt"), "staged");
            GitletCli.Run(_dir, "add", "a.txt");
        }

        // Act
        var result = GitletCli.Run(_dir, "checkout", "--", "a.txt");

        // Assert
        result.Output.ShouldBe(ExceptionMessages.FileDoesNotExist + Environment.NewLine);
        result.Error.ShouldBeEmpty();
        result.ExitCode.ShouldBe(0);

        //工作区的文件没被动过
        File.Exists(Path.Combine(_dir, "a.txt")).ShouldBe(staged);
    }

}
