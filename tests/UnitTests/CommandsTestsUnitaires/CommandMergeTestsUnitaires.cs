using System.Text;
using Gitlet.Commands;
using Shouldly;

namespace Gitlet.Tests.UnitTests;

public class CommandMergeTestsUnitaires : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "gitlet-merge-" + Guid.NewGuid());
    private readonly GitletContexte _contexte;
    private readonly CommandMerge _merge = new();

    public CommandMergeTestsUnitaires()
    {
        Directory.CreateDirectory(_dir);

        _contexte = new GitletContexte(_dir);

        // 每个测试都从一个刚 init 的仓库开始
        new CommandInit().Execute(_contexte, ["init"]);
    }

    public void Dispose()
    {
        if (Directory.Exists(_dir))
        {
            Directory.Delete(_dir, recursive: true);
        }
    }

    [Fact]
    public void ExpectedArgs_IsTwo()
    {
        // Act & Assert
        _merge.ExpectedArgs.ShouldBe(2);
    }

    //前置检查:分支不存在 / 合并自己 / 暂存区非空
    [Theory]
    [InlineData("nope", ExceptionMessages.BranchNoExists)]
    [InlineData("main", ExceptionMessages.CannotMergeWithItself)]
    [InlineData("dev", ExceptionMessages.UncommittedChanges)]
    public void Execute_FailedPrecondition_Throws(string givenBranch, string expectedMessage)
    {
        // Arrange
        File.WriteAllText(Path.Combine(_dir, "a.txt"), "a");
        new CommandAdd().Execute(_contexte, ["add", "a.txt"]);
        new CommandCommit().Execute(_contexte, ["commit", "base"]);

        new CommandBranch().Execute(_contexte, ["branch", "dev"]);

        //暂存一个新文件,暂存区非空
        File.WriteAllText(Path.Combine(_dir, "staged.txt"), "staged");
        new CommandAdd().Execute(_contexte, ["add", "staged.txt"]);

        // Act
        var exception = Should.Throw<GitletException>(() => _merge.Execute(_contexte, ["merge", givenBranch]));

        // Assert
        exception.Message.ShouldBe(expectedMessage);
    }

    //暂存区只有待删除的文件,也算未提交的改动
    [Fact]
    public void Execute_WithStagedRemoval_Throws()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_dir, "a.txt"), "a");
        new CommandAdd().Execute(_contexte, ["add", "a.txt"]);
        new CommandCommit().Execute(_contexte, ["commit", "base"]);

        new CommandBranch().Execute(_contexte, ["branch", "dev"]);

        new CommandRm().Execute(_contexte, ["rm", "a.txt"]);

        // Act
        var exception = Should.Throw<GitletException>(() => _merge.Execute(_contexte, ["merge", "dev"]));

        // Assert
        exception.Message.ShouldBe(ExceptionMessages.UncommittedChanges);
    }

    //给定分支是当前分支的祖先
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void Execute_GivenBranchIsAncestor_Throws(int commitsAhead)
    {
        // Arrange
        File.WriteAllText(Path.Combine(_dir, "base.txt"), "base");
        new CommandAdd().Execute(_contexte, ["add", "base.txt"]);
        new CommandCommit().Execute(_contexte, ["commit", "base"]);

        new CommandBranch().Execute(_contexte, ["branch", "dev"]);

        //main 往前走,dev 留在原地
        for (var i = 0; i < commitsAhead; i++)
        {
            File.WriteAllText(Path.Combine(_dir, $"main{i}.txt"), $"main{i}");
            new CommandAdd().Execute(_contexte, ["add", $"main{i}.txt"]);
            new CommandCommit().Execute(_contexte, ["commit", $"main {i}"]);
        }

        var mainHash = _contexte.Repository.GetCommitHash();

        // Act
        var exception = Should.Throw<GitletException>(() => _merge.Execute(_contexte, ["merge", "dev"]));

        // Assert
        exception.Message.ShouldBe(ExceptionMessages.GivenIsAncestor);

        _contexte.Repository.GetCommitHash().ShouldBe(mainHash);
    }

    //当前分支是给定分支的祖先:快进
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void Execute_CurrentBranchIsAncestor_FastForwards(int commitsAhead)
    {
        // Arrange
        File.WriteAllText(Path.Combine(_dir, "base.txt"), "base");
        new CommandAdd().Execute(_contexte, ["add", "base.txt"]);
        new CommandCommit().Execute(_contexte, ["commit", "base"]);

        new CommandBranch().Execute(_contexte, ["branch", "dev"]);
        new CommandCheckoutBranch().Execute(_contexte, ["checkout", "dev"]);

        //dev 往前走,main 留在原地
        for (var i = 0; i < commitsAhead; i++)
        {
            File.WriteAllText(Path.Combine(_dir, $"dev{i}.txt"), $"dev{i}");
            new CommandAdd().Execute(_contexte, ["add", $"dev{i}.txt"]);
            new CommandCommit().Execute(_contexte, ["commit", $"dev {i}"]);
        }

        var devHash = _contexte.Repository.GetCommitHash();

        new CommandCheckoutBranch().Execute(_contexte, ["checkout", "main"]);

        // Act
        var exception = Should.Throw<GitletException>(() => _merge.Execute(_contexte, ["merge", "dev"]));

        // Assert
        exception.Message.ShouldBe(ExceptionMessages.FastForwarded);

        //main 指向 dev 的 commit
        _contexte.Repository.CurrentBranch().ShouldBe("main");
        _contexte.Repository.GetCommitHash().ShouldBe(devHash);

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
    public void Execute_UntrackedFileInTheWay_Throws(string untrackedContent)
    {
        // Arrange
        File.WriteAllText(Path.Combine(_dir, "base.txt"), "base");
        new CommandAdd().Execute(_contexte, ["add", "base.txt"]);
        new CommandCommit().Execute(_contexte, ["commit", "base"]);

        new CommandBranch().Execute(_contexte, ["branch", "dev"]);

        //dev 新增 new.txt
        new CommandCheckoutBranch().Execute(_contexte, ["checkout", "dev"]);
        File.WriteAllText(Path.Combine(_dir, "new.txt"), "given");
        new CommandAdd().Execute(_contexte, ["add", "new.txt"]);
        new CommandCommit().Execute(_contexte, ["commit", "dev adds new"]);

        //main 也往前走,保证不是快进
        new CommandCheckoutBranch().Execute(_contexte, ["checkout", "main"]);
        File.WriteAllText(Path.Combine(_dir, "main.txt"), "main");
        new CommandAdd().Execute(_contexte, ["add", "main.txt"]);
        new CommandCommit().Execute(_contexte, ["commit", "main adds main"]);

        var mainHash = _contexte.Repository.GetCommitHash();

        //工作区放一个未跟踪的同名文件
        File.WriteAllText(Path.Combine(_dir, "new.txt"), untrackedContent);

        // Act
        var exception = Should.Throw<GitletException>(() => _merge.Execute(_contexte, ["merge", "dev"]));

        // Assert
        exception.Message.ShouldBe(ExceptionMessages.UntrackedFileInTheWay);

        //文件没被覆盖,也没产生 merge commit
        File.ReadAllText(Path.Combine(_dir, "new.txt")).ShouldBe(untrackedContent);
        _contexte.Repository.GetCommitHash().ShouldBe(mainHash);
    }

    //文件在 split、当前、给定三处都存在
    [Theory]
    [InlineData("a", "a", "b", "b")] //只有给定分支改了 → 取给定
    [InlineData("a", "b", "a", "b")] //只有当前分支改了 → 保持当前
    [InlineData("a", "b", "b", "b")] //两边改成一样 → 不冲突
    [InlineData("a", "a", "a", "a")] //两边都没改 → 保持原样
    public void Execute_FileModified_TakesChangedVersion(string split, string curr, string given, string expected)
    {
        // Arrange
        File.WriteAllText(Path.Combine(_dir, "f.txt"), split);
        new CommandAdd().Execute(_contexte, ["add", "f.txt"]);
        new CommandCommit().Execute(_contexte, ["commit", "base"]);

        new CommandBranch().Execute(_contexte, ["branch", "dev"]);

        //dev 上的版本
        new CommandCheckoutBranch().Execute(_contexte, ["checkout", "dev"]);
        File.WriteAllText(Path.Combine(_dir, "dev.txt"), "dev");
        new CommandAdd().Execute(_contexte, ["add", "dev.txt"]);
        File.WriteAllText(Path.Combine(_dir, "f.txt"), given);
        new CommandAdd().Execute(_contexte, ["add", "f.txt"]);
        new CommandCommit().Execute(_contexte, ["commit", "dev commit"]);

        //main 上的版本
        new CommandCheckoutBranch().Execute(_contexte, ["checkout", "main"]);
        File.WriteAllText(Path.Combine(_dir, "main.txt"), "main");
        new CommandAdd().Execute(_contexte, ["add", "main.txt"]);
        File.WriteAllText(Path.Combine(_dir, "f.txt"), curr);
        new CommandAdd().Execute(_contexte, ["add", "f.txt"]);
        new CommandCommit().Execute(_contexte, ["commit", "main commit"]);

        // Act
        var result = _merge.Execute(_contexte, ["merge", "dev"]);

        // Assert
        result.ShouldBe("dev merged into main successfully.");

        File.ReadAllText(Path.Combine(_dir, "f.txt")).ShouldBe(expected);

        //merge commit 里记录的也是这个版本
        var blobHash = _contexte.Repository.ReadCommit().Files["f.txt"];
        Encoding.UTF8.GetString(_contexte.Objects.Read("blob", blobHash)).ShouldBe(expected);
    }

    //文件在 split 存在,之后被删除(另一边没改)
    [Theory]
    [InlineData(false, true)] //给定分支删了 → 删除并取消跟踪
    [InlineData(true, false)] //当前分支删了 → 保持删除
    [InlineData(true, true)]  //两边都删了 → 保持删除
    public void Execute_FileDeleted_StaysDeleted(bool deletedInCurr, bool deletedInGiven)
    {
        // Arrange
        File.WriteAllText(Path.Combine(_dir, "f.txt"), "a");
        new CommandAdd().Execute(_contexte, ["add", "f.txt"]);
        new CommandCommit().Execute(_contexte, ["commit", "base"]);

        new CommandBranch().Execute(_contexte, ["branch", "dev"]);

        //dev
        new CommandCheckoutBranch().Execute(_contexte, ["checkout", "dev"]);
        File.WriteAllText(Path.Combine(_dir, "dev.txt"), "dev");
        new CommandAdd().Execute(_contexte, ["add", "dev.txt"]);
        if (deletedInGiven)
        {
            new CommandRm().Execute(_contexte, ["rm", "f.txt"]);
        }
        new CommandCommit().Execute(_contexte, ["commit", "dev commit"]);

        //main
        new CommandCheckoutBranch().Execute(_contexte, ["checkout", "main"]);
        File.WriteAllText(Path.Combine(_dir, "main.txt"), "main");
        new CommandAdd().Execute(_contexte, ["add", "main.txt"]);
        if (deletedInCurr)
        {
            new CommandRm().Execute(_contexte, ["rm", "f.txt"]);
        }
        new CommandCommit().Execute(_contexte, ["commit", "main commit"]);

        // Act
        _merge.Execute(_contexte, ["merge", "dev"]);

        // Assert
        File.Exists(Path.Combine(_dir, "f.txt")).ShouldBeFalse();

        _contexte.Repository.ReadCommit().Files.ContainsKey("f.txt").ShouldBeFalse();
    }

    //文件在 split 不存在,之后才新增(null 表示这一边没有这个文件)
    [Theory]
    [InlineData(null, "b", "b")] //只在给定分支新增 → 检出
    [InlineData("b", null, "b")] //只在当前分支新增 → 保持原样
    [InlineData("b", "b", "b")]  //两边新增的内容一样 → 不冲突
    public void Execute_FileAddedAfterSplit_IsKept(string? curr, string? given, string expected)
    {
        // Arrange
        File.WriteAllText(Path.Combine(_dir, "base.txt"), "base");
        new CommandAdd().Execute(_contexte, ["add", "base.txt"]);
        new CommandCommit().Execute(_contexte, ["commit", "base"]);

        new CommandBranch().Execute(_contexte, ["branch", "dev"]);

        //dev
        new CommandCheckoutBranch().Execute(_contexte, ["checkout", "dev"]);
        File.WriteAllText(Path.Combine(_dir, "dev.txt"), "dev");
        new CommandAdd().Execute(_contexte, ["add", "dev.txt"]);
        if (given != null)
        {
            File.WriteAllText(Path.Combine(_dir, "f.txt"), given);
            new CommandAdd().Execute(_contexte, ["add", "f.txt"]);
        }
        new CommandCommit().Execute(_contexte, ["commit", "dev commit"]);

        //main
        new CommandCheckoutBranch().Execute(_contexte, ["checkout", "main"]);
        File.WriteAllText(Path.Combine(_dir, "main.txt"), "main");
        new CommandAdd().Execute(_contexte, ["add", "main.txt"]);
        if (curr != null)
        {
            File.WriteAllText(Path.Combine(_dir, "f.txt"), curr);
            new CommandAdd().Execute(_contexte, ["add", "f.txt"]);
        }
        new CommandCommit().Execute(_contexte, ["commit", "main commit"]);

        // Act
        _merge.Execute(_contexte, ["merge", "dev"]);

        // Assert
        File.ReadAllText(Path.Combine(_dir, "f.txt")).ShouldBe(expected);

        var blobHash = _contexte.Repository.ReadCommit().Files["f.txt"];
        Encoding.UTF8.GetString(_contexte.Objects.Read("blob", blobHash)).ShouldBe(expected);
    }

    //冲突(null 表示这一处没有这个文件)
    [Theory]
    [InlineData("a\n", "b\n", "c\n", "<<<<<<< HEAD\nb\n=======\nc\n>>>>>>>\n")] //两边改得不一样
    [InlineData("a\n", null, "c\n", "<<<<<<< HEAD\n=======\nc\n>>>>>>>\n")]     //当前删了,给定改了
    [InlineData(null, "b\n", "c\n", "<<<<<<< HEAD\nb\n=======\nc\n>>>>>>>\n")]  //两边新增的内容不一样
    public void Execute_Conflict_WritesConflictFileAndThrows(string? split, string? curr, string given, string expected)
    {
        // Arrange
        File.WriteAllText(Path.Combine(_dir, "base.txt"), "base");
        new CommandAdd().Execute(_contexte, ["add", "base.txt"]);
        if (split != null)
        {
            File.WriteAllText(Path.Combine(_dir, "f.txt"), split);
            new CommandAdd().Execute(_contexte, ["add", "f.txt"]);
        }
        new CommandCommit().Execute(_contexte, ["commit", "base"]);

        new CommandBranch().Execute(_contexte, ["branch", "dev"]);

        //dev
        new CommandCheckoutBranch().Execute(_contexte, ["checkout", "dev"]);
        File.WriteAllText(Path.Combine(_dir, "f.txt"), given);
        new CommandAdd().Execute(_contexte, ["add", "f.txt"]);
        new CommandCommit().Execute(_contexte, ["commit", "dev commit"]);

        var devHash = _contexte.Repository.GetCommitHash();

        //main
        new CommandCheckoutBranch().Execute(_contexte, ["checkout", "main"]);
        File.WriteAllText(Path.Combine(_dir, "main.txt"), "main");
        new CommandAdd().Execute(_contexte, ["add", "main.txt"]);
        if (curr != null)
        {
            File.WriteAllText(Path.Combine(_dir, "f.txt"), curr);
            new CommandAdd().Execute(_contexte, ["add", "f.txt"]);
        }
        else
        {
            new CommandRm().Execute(_contexte, ["rm", "f.txt"]);
        }
        new CommandCommit().Execute(_contexte, ["commit", "main commit"]);

        var mainHash = _contexte.Repository.GetCommitHash();

        // Act
        var exception = Should.Throw<GitletException>(() => _merge.Execute(_contexte, ["merge", "dev"]));

        // Assert
        exception.Message.ShouldBe(ExceptionMessages.MergeConflict);

        //工作区写入了冲突内容
        File.ReadAllText(Path.Combine(_dir, "f.txt")).ShouldBe(expected);

        //冲突时仍然生成 merge commit,里面记录的是冲突内容
        var mergeCommit = _contexte.Repository.ReadCommit();
        mergeCommit.ParentHashes.ShouldBe([mainHash, devHash]);

        var blobHash = mergeCommit.Files["f.txt"];
        Encoding.UTF8.GetString(_contexte.Objects.Read("blob", blobHash)).ShouldBe(expected);
    }

    //merge commit 本身:消息、两个父、两边的文件都在
    [Theory]
    [InlineData("dev")]
    [InlineData("feature")]
    [InlineData("fix-1")]
    public void Execute_CreatesMergeCommitWithTwoParents(string givenBranch)
    {
        // Arrange
        File.WriteAllText(Path.Combine(_dir, "base.txt"), "base");
        new CommandAdd().Execute(_contexte, ["add", "base.txt"]);
        new CommandCommit().Execute(_contexte, ["commit", "base"]);

        new CommandBranch().Execute(_contexte, ["branch", givenBranch]);

        //给定分支
        new CommandCheckoutBranch().Execute(_contexte, ["checkout", givenBranch]);
        File.WriteAllText(Path.Combine(_dir, "given.txt"), "given");
        new CommandAdd().Execute(_contexte, ["add", "given.txt"]);
        new CommandCommit().Execute(_contexte, ["commit", "given commit"]);

        var givenHash = _contexte.Repository.GetCommitHash();

        //main
        new CommandCheckoutBranch().Execute(_contexte, ["checkout", "main"]);
        File.WriteAllText(Path.Combine(_dir, "main.txt"), "main");
        new CommandAdd().Execute(_contexte, ["add", "main.txt"]);
        new CommandCommit().Execute(_contexte, ["commit", "main commit"]);

        var mainHash = _contexte.Repository.GetCommitHash();

        // Act
        var result = _merge.Execute(_contexte, ["merge", givenBranch]);

        // Assert
        result.ShouldBe($"{givenBranch} merged into main successfully.");

        var mergeCommit = _contexte.Repository.ReadCommit();

        mergeCommit.Message.ShouldBe($"Merged {givenBranch} into main.");
        mergeCommit.ParentHashes.ShouldBe([mainHash, givenHash]);
        mergeCommit.Files.Keys.ShouldBe(["base.txt", "given.txt", "main.txt"]);

        //给定分支新增的文件被检出到工作区
        File.ReadAllText(Path.Combine(_dir, "given.txt")).ShouldBe("given");

        //当前分支还是 main,给定分支没动
        _contexte.Repository.CurrentBranch().ShouldBe("main");
        File.ReadAllText(_contexte.Paths.BranchFile(givenBranch)).ShouldBe(givenHash);
    }

    //第二次 merge:split point 要沿着 merge commit 的第二个父找
    [Fact]
    public void Execute_SecondMergeAfterBothBranchesAdvance_UsesLatestCommonAncestor()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_dir, "f.txt"), "a");
        new CommandAdd().Execute(_contexte, ["add", "f.txt"]);
        new CommandCommit().Execute(_contexte, ["commit", "base"]);

        new CommandBranch().Execute(_contexte, ["branch", "dev"]);

        //dev 把 f.txt 改成 b
        new CommandCheckoutBranch().Execute(_contexte, ["checkout", "dev"]);
        File.WriteAllText(Path.Combine(_dir, "f.txt"), "b");
        new CommandAdd().Execute(_contexte, ["add", "f.txt"]);
        new CommandCommit().Execute(_contexte, ["commit", "dev changes f"]);

        //main 往前走,然后第一次 merge
        new CommandCheckoutBranch().Execute(_contexte, ["checkout", "main"]);
        File.WriteAllText(Path.Combine(_dir, "main.txt"), "main");
        new CommandAdd().Execute(_contexte, ["add", "main.txt"]);
        new CommandCommit().Execute(_contexte, ["commit", "main commit"]);

        _merge.Execute(_contexte, ["merge", "dev"]);

        //dev 再把 f.txt 改成 c
        new CommandCheckoutBranch().Execute(_contexte, ["checkout", "dev"]);
        File.WriteAllText(Path.Combine(_dir, "f.txt"), "c");
        new CommandAdd().Execute(_contexte, ["add", "f.txt"]);
        new CommandCommit().Execute(_contexte, ["commit", "dev changes f again"]);

        new CommandCheckoutBranch().Execute(_contexte, ["checkout", "main"]);

        // Act
        _merge.Execute(_contexte, ["merge", "dev"]);

        // Assert
        //split 是 "dev changes f"(f=b),main 没再改 f → 取 dev 的 c,不冲突
        File.ReadAllText(Path.Combine(_dir, "f.txt")).ShouldBe("c");

        File.ReadAllText(Path.Combine(_dir, "main.txt")).ShouldBe("main");
    }
}
