using Shouldly;

namespace Gitlet.Tests.IntegrationTests;

public class GitletMergeScopeIntegrationTests : IDisposable
{

    private readonly string _dir = Path.Combine(Path.GetTempPath(), "gitlet-merge-cli-" + Guid.NewGuid());

    //constructor
    public GitletMergeScopeIntegrationTests()
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
    [InlineData("merge")]
    [InlineData("merge", "dev", "extra")]
    [InlineData("merge", "dev", "extra", "more")]
    public void Merge_WrongNumberOfArguments_IsIncorrectOperands(params string[] args)
    {
        // Arrange
        GitletCli.Run(_dir, "init");

        // Act
        var result = GitletCli.Run(_dir, args);

        // Assert
        result.Output.ShouldBe(ExceptionMessages.IncorrectOperands + Environment.NewLine);
    }

    //还没 init 就 merge
    [Fact]
    public void Merge_BeforeInit_IsRejected()
    {
        // Act
        var result = GitletCli.Run(_dir, "merge", "dev");

        // Assert
        result.Output.ShouldBe(ExceptionMessages.NotInitialized + Environment.NewLine);

        Directory.Exists(Path.Combine(_dir, ".gitlet")).ShouldBeFalse();
    }

    //前置检查:分支不存在 / 合并自己 / 暂存区非空
    [Theory]
    [InlineData("nope", ExceptionMessages.BranchNoExists)]
    [InlineData("main", ExceptionMessages.CannotMergeWithItself)]
    [InlineData("dev", ExceptionMessages.UncommittedChanges)]
    public void Merge_FailedPrecondition_PrintsOnlyTheMessage(string givenBranch, string expectedMessage)
    {
        // Arrange
        GitletCli.Run(_dir, "init");

        File.WriteAllText(Path.Combine(_dir, "a.txt"), "a");
        GitletCli.Run(_dir, "add", "a.txt");
        GitletCli.Run(_dir, "commit", "base");

        GitletCli.Run(_dir, "branch", "dev");

        //暂存一个新文件,暂存区非空
        File.WriteAllText(Path.Combine(_dir, "staged.txt"), "staged");
        GitletCli.Run(_dir, "add", "staged.txt");

        // Act
        var result = GitletCli.Run(_dir, "merge", givenBranch);

        // Assert
        result.Output.ShouldBe(expectedMessage + Environment.NewLine);
        result.Error.ShouldBeEmpty();
        result.ExitCode.ShouldBe(0);
    }

    //给定分支是当前分支的祖先
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void Merge_GivenBranchIsAncestor_PrintsMessageAndChangesNothing(int commitsAhead)
    {
        // Arrange
        GitletCli.Run(_dir, "init");

        File.WriteAllText(Path.Combine(_dir, "base.txt"), "base");
        GitletCli.Run(_dir, "add", "base.txt");
        GitletCli.Run(_dir, "commit", "base");

        GitletCli.Run(_dir, "branch", "dev");

        //main 往前走,dev 留在原地
        for (var i = 0; i < commitsAhead; i++)
        {
            File.WriteAllText(Path.Combine(_dir, $"main{i}.txt"), $"main{i}");
            GitletCli.Run(_dir, "add", $"main{i}.txt");
            GitletCli.Run(_dir, "commit", $"main {i}");
        }

        var logBefore = GitletCli.Run(_dir, "log").Output;

        // Act
        var result = GitletCli.Run(_dir, "merge", "dev");

        // Assert
        result.Output.ShouldBe(ExceptionMessages.GivenIsAncestor + Environment.NewLine);

        GitletCli.Run(_dir, "log").Output.ShouldBe(logBefore);
    }

    //当前分支是给定分支的祖先:快进
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void Merge_CurrentBranchIsAncestor_FastForwards(int commitsAhead)
    {
        // Arrange
        GitletCli.Run(_dir, "init");

        File.WriteAllText(Path.Combine(_dir, "base.txt"), "base");
        GitletCli.Run(_dir, "add", "base.txt");
        GitletCli.Run(_dir, "commit", "base");

        GitletCli.Run(_dir, "branch", "dev");
        GitletCli.Run(_dir, "checkout", "dev");

        //dev 往前走,main 留在原地
        for (var i = 0; i < commitsAhead; i++)
        {
            File.WriteAllText(Path.Combine(_dir, $"dev{i}.txt"), $"dev{i}");
            GitletCli.Run(_dir, "add", $"dev{i}.txt");
            GitletCli.Run(_dir, "commit", $"dev {i}");
        }

        var devLog = GitletCli.Run(_dir, "log").Output;

        GitletCli.Run(_dir, "checkout", "main");

        // Act
        var result = GitletCli.Run(_dir, "merge", "dev");

        // Assert
        result.Output.ShouldBe(ExceptionMessages.FastForwarded + Environment.NewLine);

        //main 的历史和 dev 完全一样,没有多出 merge commit
        GitletCli.Run(_dir, "log").Output.ShouldBe(devLog);
        GitletCli.Run(_dir, "status").Output.ShouldContain("*main\n");

        //dev 的文件都被检出
        for (var i = 0; i < commitsAhead; i++)
        {
            File.ReadAllText(Path.Combine(_dir, $"dev{i}.txt")).ShouldBe($"dev{i}");
        }
    }

    //未跟踪文件挡路:内容相同、不同、为空都不允许覆盖
    [Theory]
    [InlineData("given")]
    [InlineData("other")]
    [InlineData("")]
    public void Merge_UntrackedFileInTheWay_PrintsMessageAndKeepsFile(string untrackedContent)
    {
        // Arrange
        GitletCli.Run(_dir, "init");

        File.WriteAllText(Path.Combine(_dir, "base.txt"), "base");
        GitletCli.Run(_dir, "add", "base.txt");
        GitletCli.Run(_dir, "commit", "base");

        GitletCli.Run(_dir, "branch", "dev");

        //dev 新增 new.txt
        GitletCli.Run(_dir, "checkout", "dev");
        File.WriteAllText(Path.Combine(_dir, "new.txt"), "given");
        GitletCli.Run(_dir, "add", "new.txt");
        GitletCli.Run(_dir, "commit", "dev adds new");

        //main 也往前走,保证不是快进
        GitletCli.Run(_dir, "checkout", "main");
        File.WriteAllText(Path.Combine(_dir, "main.txt"), "main");
        GitletCli.Run(_dir, "add", "main.txt");
        GitletCli.Run(_dir, "commit", "main adds main");

        var logBefore = GitletCli.Run(_dir, "log").Output;

        //工作区放一个未跟踪的同名文件
        File.WriteAllText(Path.Combine(_dir, "new.txt"), untrackedContent);

        // Act
        var result = GitletCli.Run(_dir, "merge", "dev");

        // Assert
        result.Output.ShouldBe(ExceptionMessages.UntrackedFileInTheWay + Environment.NewLine);

        //文件没被覆盖,也没产生 merge commit
        File.ReadAllText(Path.Combine(_dir, "new.txt")).ShouldBe(untrackedContent);
        GitletCli.Run(_dir, "log").Output.ShouldBe(logBefore);
    }

    //没有冲突的合并:什么都不打印,生成 merge commit
    [Theory]
    [InlineData("a", "a", "b", "b")] //只有给定分支改了 → 取给定
    [InlineData("a", "b", "a", "b")] //只有当前分支改了 → 保持当前
    [InlineData("a", "b", "b", "b")] //两边改成一样 → 不冲突
    public void Merge_WithoutConflict_PrintsNothingAndCommits(string split, string curr, string given, string expected)
    {
        // Arrange
        GitletCli.Run(_dir, "init");

        File.WriteAllText(Path.Combine(_dir, "f.txt"), split);
        GitletCli.Run(_dir, "add", "f.txt");
        GitletCli.Run(_dir, "commit", "base");

        GitletCli.Run(_dir, "branch", "dev");

        //dev 上的版本
        GitletCli.Run(_dir, "checkout", "dev");
        File.WriteAllText(Path.Combine(_dir, "dev.txt"), "dev");
        GitletCli.Run(_dir, "add", "dev.txt");
        File.WriteAllText(Path.Combine(_dir, "f.txt"), given);
        GitletCli.Run(_dir, "add", "f.txt");
        GitletCli.Run(_dir, "commit", "dev commit");

        //main 上的版本
        GitletCli.Run(_dir, "checkout", "main");
        File.WriteAllText(Path.Combine(_dir, "main.txt"), "main");
        GitletCli.Run(_dir, "add", "main.txt");
        File.WriteAllText(Path.Combine(_dir, "f.txt"), curr);
        GitletCli.Run(_dir, "add", "f.txt");
        GitletCli.Run(_dir, "commit", "main commit");

        // Act
        var result = GitletCli.Run(_dir, "merge", "dev");

        // Assert
        result.Output.ShouldBeEmpty();
        result.Error.ShouldBeEmpty();

        //工作区:合并后的版本 + 两边各自新增的文件
        File.ReadAllText(Path.Combine(_dir, "f.txt")).ShouldBe(expected);
        File.ReadAllText(Path.Combine(_dir, "dev.txt")).ShouldBe("dev");
        File.ReadAllText(Path.Combine(_dir, "main.txt")).ShouldBe("main");

        //log 最上面是 merge commit
        GitletCli.Run(_dir, "log").Output.ShouldContain("Merged dev into main.");

        //合并后的版本已经进了 commit:改坏再 checkout 能恢复
        File.WriteAllText(Path.Combine(_dir, "f.txt"), "broken");
        GitletCli.Run(_dir, "checkout", "--", "f.txt");
        File.ReadAllText(Path.Combine(_dir, "f.txt")).ShouldBe(expected);
    }

    //冲突(null 表示这一处没有这个文件)
    [Theory]
    [InlineData("a\n", "b\n", "c\n", "<<<<<<< HEAD\nb\n=======\nc\n>>>>>>>\n")] //两边改得不一样
    [InlineData("a\n", null, "c\n", "<<<<<<< HEAD\n=======\nc\n>>>>>>>\n")]     //当前删了,给定改了
    [InlineData(null, "b\n", "c\n", "<<<<<<< HEAD\nb\n=======\nc\n>>>>>>>\n")]  //两边新增的内容不一样
    public void Merge_Conflict_PrintsMessageAndWritesConflictFile(string? split, string? curr, string given, string expected)
    {
        // Arrange
        GitletCli.Run(_dir, "init");

        File.WriteAllText(Path.Combine(_dir, "base.txt"), "base");
        GitletCli.Run(_dir, "add", "base.txt");
        if (split != null)
        {
            File.WriteAllText(Path.Combine(_dir, "f.txt"), split);
            GitletCli.Run(_dir, "add", "f.txt");
        }
        GitletCli.Run(_dir, "commit", "base");

        GitletCli.Run(_dir, "branch", "dev");

        //dev
        GitletCli.Run(_dir, "checkout", "dev");
        File.WriteAllText(Path.Combine(_dir, "f.txt"), given);
        GitletCli.Run(_dir, "add", "f.txt");
        GitletCli.Run(_dir, "commit", "dev commit");

        //main
        GitletCli.Run(_dir, "checkout", "main");
        File.WriteAllText(Path.Combine(_dir, "main.txt"), "main");
        GitletCli.Run(_dir, "add", "main.txt");
        if (curr != null)
        {
            File.WriteAllText(Path.Combine(_dir, "f.txt"), curr);
            GitletCli.Run(_dir, "add", "f.txt");
        }
        else
        {
            GitletCli.Run(_dir, "rm", "f.txt");
        }
        GitletCli.Run(_dir, "commit", "main commit");

        // Act
        var result = GitletCli.Run(_dir, "merge", "dev");

        // Assert
        result.Output.ShouldBe(ExceptionMessages.MergeConflict + Environment.NewLine);
        result.Error.ShouldBeEmpty();

        //工作区写入了冲突内容
        File.ReadAllText(Path.Combine(_dir, "f.txt")).ShouldBe(expected);

        //冲突时仍然生成 merge commit
        GitletCli.Run(_dir, "log").Output.ShouldContain("Merged dev into main.");
    }

    //合并之后继续工作:两个分支各自再提交,再合并一次
    [Theory]
    [InlineData("dev")]
    [InlineData("feature")]
    [InlineData("fix-1")]
    public void Scenario_MergeTwice_SecondMergeUsesNewSplitPoint(string givenBranch)
    {
        // Arrange
        GitletCli.Run(_dir, "init");

        File.WriteAllText(Path.Combine(_dir, "f.txt"), "v1");
        GitletCli.Run(_dir, "add", "f.txt");
        GitletCli.Run(_dir, "commit", "base");

        GitletCli.Run(_dir, "branch", givenBranch);

        //给定分支改 f.txt
        GitletCli.Run(_dir, "checkout", givenBranch);
        File.WriteAllText(Path.Combine(_dir, "f.txt"), "v2");
        GitletCli.Run(_dir, "add", "f.txt");
        GitletCli.Run(_dir, "commit", "given v2");

        //main 加一个自己的文件
        GitletCli.Run(_dir, "checkout", "main");
        File.WriteAllText(Path.Combine(_dir, "main.txt"), "main");
        GitletCli.Run(_dir, "add", "main.txt");
        GitletCli.Run(_dir, "commit", "main commit");

        // Act
        var firstMerge = GitletCli.Run(_dir, "merge", givenBranch);

        // Assert
        firstMerge.Output.ShouldBeEmpty();
        File.ReadAllText(Path.Combine(_dir, "f.txt")).ShouldBe("v2");

        // Arrange
        //给定分支再改一次 f.txt
        GitletCli.Run(_dir, "checkout", givenBranch);
        File.WriteAllText(Path.Combine(_dir, "f.txt"), "v3");
        GitletCli.Run(_dir, "add", "f.txt");
        GitletCli.Run(_dir, "commit", "given v3");

        //main 再加一个文件
        GitletCli.Run(_dir, "checkout", "main");
        File.WriteAllText(Path.Combine(_dir, "main2.txt"), "main2");
        GitletCli.Run(_dir, "add", "main2.txt");
        GitletCli.Run(_dir, "commit", "main commit 2");

        // Act
        var secondMerge = GitletCli.Run(_dir, "merge", givenBranch);

        // Assert
        //split point 是给定分支的 v2,main 的 f.txt 没再改 → 不冲突,取 v3
        secondMerge.Output.ShouldBeEmpty();
        File.ReadAllText(Path.Combine(_dir, "f.txt")).ShouldBe("v3");
        File.ReadAllText(Path.Combine(_dir, "main.txt")).ShouldBe("main");
        File.ReadAllText(Path.Combine(_dir, "main2.txt")).ShouldBe("main2");

        //暂存区是空的,当前分支还是 main
        var status = GitletCli.Run(_dir, "status").Output;
        status.ShouldContain("*main\n");
        status.ShouldContain("=== Staged Files ===\n\n");
        status.ShouldContain("=== Removed Files ===\n\n");

        //给定分支上的提交在全局历史里
        GitletCli.Run(_dir, "global-log").Output.ShouldContain("given v3");
    }

}
