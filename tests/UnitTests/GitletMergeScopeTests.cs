using System.Text;
using Shouldly;

namespace Gitlet.Tests.UnitTests;

public class GitletMergeScopeTests : IDisposable
{

    private readonly string _dir = Path.Combine(Path.GetTempPath(), "gitlet-merge-" + Guid.NewGuid());

    private readonly GitletPaths _paths;

    private readonly ObjectStore _store;

    private readonly RepositoryState _state;

    private readonly Repository _repo;

    private readonly GitletMergeScope _merge;

    //constructor
    public GitletMergeScopeTests()
    {
        Directory.CreateDirectory(_dir);

        _paths = new GitletPaths(_dir);
        _store = new ObjectStore(_paths);
        _state = new RepositoryState(_paths, _store, _dir);
        _repo = new Repository(_dir);
        _merge = new GitletMergeScope(_dir);
    }

    //helpers 清理每次测试后产生的文件
    public void Dispose()
    {
        if (Directory.Exists(_dir))
        {
            Directory.Delete(_dir, recursive: true);
        }
    }

    //scenario 每个if写一个scenario

    //前置检查:分支不存在 / 合并自己 / 暂存区非空
    [Theory]
    [InlineData("nope", ExceptionMessages.BranchNoExists)]
    [InlineData("main", ExceptionMessages.CannotMergeWithItself)]
    [InlineData("dev", ExceptionMessages.UncommittedChanges)]
    public void Merge_FailedPrecondition_Throws(string givenBranch, string expectedMessage)
    {
        // Arrange
        _repo.Init();

        File.WriteAllText(Path.Combine(_dir, "a.txt"), "a");
        _repo.GitletAdd("a.txt");
        _repo.GitletCommit("base");

        _repo.GitletBranch("dev");

        //暂存一个新文件,暂存区非空
        File.WriteAllText(Path.Combine(_dir, "staged.txt"), "staged");
        _repo.GitletAdd("staged.txt");

        // Act
        var exception = Should.Throw<GitletException>(() => _merge.GitletMerge(givenBranch));

        // Assert
        exception.Message.ShouldBe(expectedMessage);
    }

    //暂存区只有待删除的文件,也算未提交的改动
    [Fact]
    public void Merge_WithStagedRemoval_Throws()
    {
        // Arrange
        _repo.Init();

        File.WriteAllText(Path.Combine(_dir, "a.txt"), "a");
        _repo.GitletAdd("a.txt");
        _repo.GitletCommit("base");

        _repo.GitletBranch("dev");

        _repo.GitletDelete("a.txt");

        // Act
        var exception = Should.Throw<GitletException>(() => _merge.GitletMerge("dev"));

        // Assert
        exception.Message.ShouldBe(ExceptionMessages.UncommittedChanges);
    }

    //给定分支是当前分支的祖先
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void Merge_GivenBranchIsAncestor_Throws(int commitsAhead)
    {
        // Arrange
        _repo.Init();

        File.WriteAllText(Path.Combine(_dir, "base.txt"), "base");
        _repo.GitletAdd("base.txt");
        _repo.GitletCommit("base");

        _repo.GitletBranch("dev");

        //main 往前走,dev 留在原地
        for (var i = 0; i < commitsAhead; i++)
        {
            File.WriteAllText(Path.Combine(_dir, $"main{i}.txt"), $"main{i}");
            _repo.GitletAdd($"main{i}.txt");
            _repo.GitletCommit($"main {i}");
        }

        var mainHash = _state.GetCommitHash();

        // Act
        var exception = Should.Throw<GitletException>(() => _merge.GitletMerge("dev"));

        // Assert
        exception.Message.ShouldBe(ExceptionMessages.GivenIsAncestor);

        _state.GetCommitHash().ShouldBe(mainHash);
    }

    //当前分支是给定分支的祖先:快进
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void Merge_CurrentBranchIsAncestor_FastForwards(int commitsAhead)
    {
        // Arrange
        _repo.Init();

        File.WriteAllText(Path.Combine(_dir, "base.txt"), "base");
        _repo.GitletAdd("base.txt");
        _repo.GitletCommit("base");

        _repo.GitletBranch("dev");
        _repo.GitletCheckoutBranch("dev");

        //dev 往前走,main 留在原地
        for (var i = 0; i < commitsAhead; i++)
        {
            File.WriteAllText(Path.Combine(_dir, $"dev{i}.txt"), $"dev{i}");
            _repo.GitletAdd($"dev{i}.txt");
            _repo.GitletCommit($"dev {i}");
        }

        var devHash = _state.GetCommitHash();

        _repo.GitletCheckoutBranch("main");

        // Act
        var exception = Should.Throw<GitletException>(() => _merge.GitletMerge("dev"));

        // Assert
        exception.Message.ShouldBe(ExceptionMessages.FastForwarded);

        //main 指向 dev 的 commit
        _state.CurrentBranch().ShouldBe("main");
        _state.GetCommitHash().ShouldBe(devHash);

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
    public void Merge_UntrackedFileInTheWay_Throws(string untrackedContent)
    {
        // Arrange
        _repo.Init();

        File.WriteAllText(Path.Combine(_dir, "base.txt"), "base");
        _repo.GitletAdd("base.txt");
        _repo.GitletCommit("base");

        _repo.GitletBranch("dev");

        //dev 新增 new.txt
        _repo.GitletCheckoutBranch("dev");
        File.WriteAllText(Path.Combine(_dir, "new.txt"), "given");
        _repo.GitletAdd("new.txt");
        _repo.GitletCommit("dev adds new");

        //main 也往前走,保证不是快进
        _repo.GitletCheckoutBranch("main");
        File.WriteAllText(Path.Combine(_dir, "main.txt"), "main");
        _repo.GitletAdd("main.txt");
        _repo.GitletCommit("main adds main");

        var mainHash = _state.GetCommitHash();

        //工作区放一个未跟踪的同名文件
        File.WriteAllText(Path.Combine(_dir, "new.txt"), untrackedContent);

        // Act
        var exception = Should.Throw<GitletException>(() => _merge.GitletMerge("dev"));

        // Assert
        exception.Message.ShouldBe(ExceptionMessages.UntrackedFileInTheWay);

        //文件没被覆盖,也没产生 merge commit
        File.ReadAllText(Path.Combine(_dir, "new.txt")).ShouldBe(untrackedContent);
        _state.GetCommitHash().ShouldBe(mainHash);
    }

    //文件在 split、当前、给定三处都存在
    [Theory]
    [InlineData("a", "a", "b", "b")] //只有给定分支改了 → 取给定
    [InlineData("a", "b", "a", "b")] //只有当前分支改了 → 保持当前
    [InlineData("a", "b", "b", "b")] //两边改成一样 → 不冲突
    public void Merge_FileModified_TakesChangedVersion(string split, string curr, string given, string expected)
    {
        // Arrange
        _repo.Init();

        File.WriteAllText(Path.Combine(_dir, "f.txt"), split);
        _repo.GitletAdd("f.txt");
        _repo.GitletCommit("base");

        _repo.GitletBranch("dev");

        //dev 上的版本
        _repo.GitletCheckoutBranch("dev");
        File.WriteAllText(Path.Combine(_dir, "dev.txt"), "dev");
        _repo.GitletAdd("dev.txt");
        File.WriteAllText(Path.Combine(_dir, "f.txt"), given);
        _repo.GitletAdd("f.txt");
        _repo.GitletCommit("dev commit");

        //main 上的版本
        _repo.GitletCheckoutBranch("main");
        File.WriteAllText(Path.Combine(_dir, "main.txt"), "main");
        _repo.GitletAdd("main.txt");
        File.WriteAllText(Path.Combine(_dir, "f.txt"), curr);
        _repo.GitletAdd("f.txt");
        _repo.GitletCommit("main commit");

        // Act
        _merge.GitletMerge("dev");

        // Assert
        File.ReadAllText(Path.Combine(_dir, "f.txt")).ShouldBe(expected);

        //merge commit 里记录的也是这个版本
        var blobHash = _state.ReadCommit().Files["f.txt"];
        Encoding.UTF8.GetString(_store.Read("blob", blobHash)).ShouldBe(expected);
    }

    //文件在 split 存在,之后被删除(另一边没改)
    [Theory]
    [InlineData(false, true)] //给定分支删了 → 删除并取消跟踪
    [InlineData(true, false)] //当前分支删了 → 保持删除
    [InlineData(true, true)]  //两边都删了 → 保持删除
    public void Merge_FileDeleted_StaysDeleted(bool deletedInCurr, bool deletedInGiven)
    {
        // Arrange
        _repo.Init();

        File.WriteAllText(Path.Combine(_dir, "f.txt"), "a");
        _repo.GitletAdd("f.txt");
        _repo.GitletCommit("base");

        _repo.GitletBranch("dev");

        //dev
        _repo.GitletCheckoutBranch("dev");
        File.WriteAllText(Path.Combine(_dir, "dev.txt"), "dev");
        _repo.GitletAdd("dev.txt");
        if (deletedInGiven)
        {
            _repo.GitletDelete("f.txt");
        }
        _repo.GitletCommit("dev commit");

        //main
        _repo.GitletCheckoutBranch("main");
        File.WriteAllText(Path.Combine(_dir, "main.txt"), "main");
        _repo.GitletAdd("main.txt");
        if (deletedInCurr)
        {
            _repo.GitletDelete("f.txt");
        }
        _repo.GitletCommit("main commit");

        // Act
        _merge.GitletMerge("dev");

        // Assert
        File.Exists(Path.Combine(_dir, "f.txt")).ShouldBeFalse();

        _state.ReadCommit().Files.ContainsKey("f.txt").ShouldBeFalse();
    }

    //文件在 split 不存在,之后才新增(null 表示这一边没有这个文件)
    [Theory]
    [InlineData(null, "b", "b")] //只在给定分支新增 → 检出
    [InlineData("b", null, "b")] //只在当前分支新增 → 保持原样
    [InlineData("b", "b", "b")]  //两边新增的内容一样 → 不冲突
    public void Merge_FileAddedAfterSplit_IsKept(string? curr, string? given, string expected)
    {
        // Arrange
        _repo.Init();

        File.WriteAllText(Path.Combine(_dir, "base.txt"), "base");
        _repo.GitletAdd("base.txt");
        _repo.GitletCommit("base");

        _repo.GitletBranch("dev");

        //dev
        _repo.GitletCheckoutBranch("dev");
        File.WriteAllText(Path.Combine(_dir, "dev.txt"), "dev");
        _repo.GitletAdd("dev.txt");
        if (given != null)
        {
            File.WriteAllText(Path.Combine(_dir, "f.txt"), given);
            _repo.GitletAdd("f.txt");
        }
        _repo.GitletCommit("dev commit");

        //main
        _repo.GitletCheckoutBranch("main");
        File.WriteAllText(Path.Combine(_dir, "main.txt"), "main");
        _repo.GitletAdd("main.txt");
        if (curr != null)
        {
            File.WriteAllText(Path.Combine(_dir, "f.txt"), curr);
            _repo.GitletAdd("f.txt");
        }
        _repo.GitletCommit("main commit");

        // Act
        _merge.GitletMerge("dev");

        // Assert
        File.ReadAllText(Path.Combine(_dir, "f.txt")).ShouldBe(expected);

        var blobHash = _state.ReadCommit().Files["f.txt"];
        Encoding.UTF8.GetString(_store.Read("blob", blobHash)).ShouldBe(expected);
    }

    //冲突(null 表示这一处没有这个文件)
    [Theory]
    [InlineData("a\n", "b\n", "c\n", "<<<<<<< HEAD\nb\n=======\nc\n>>>>>>>\n")] //两边改得不一样
    [InlineData("a\n", null, "c\n", "<<<<<<< HEAD\n=======\nc\n>>>>>>>\n")]     //当前删了,给定改了
    [InlineData(null, "b\n", "c\n", "<<<<<<< HEAD\nb\n=======\nc\n>>>>>>>\n")]  //两边新增的内容不一样
    public void Merge_Conflict_WritesConflictFileAndThrows(string? split, string? curr, string given, string expected)
    {
        // Arrange
        _repo.Init();

        File.WriteAllText(Path.Combine(_dir, "base.txt"), "base");
        _repo.GitletAdd("base.txt");
        if (split != null)
        {
            File.WriteAllText(Path.Combine(_dir, "f.txt"), split);
            _repo.GitletAdd("f.txt");
        }
        _repo.GitletCommit("base");

        _repo.GitletBranch("dev");

        //dev
        _repo.GitletCheckoutBranch("dev");
        File.WriteAllText(Path.Combine(_dir, "f.txt"), given);
        _repo.GitletAdd("f.txt");
        _repo.GitletCommit("dev commit");

        var devHash = _state.GetCommitHash();

        //main
        _repo.GitletCheckoutBranch("main");
        File.WriteAllText(Path.Combine(_dir, "main.txt"), "main");
        _repo.GitletAdd("main.txt");
        if (curr != null)
        {
            File.WriteAllText(Path.Combine(_dir, "f.txt"), curr);
            _repo.GitletAdd("f.txt");
        }
        else
        {
            _repo.GitletDelete("f.txt");
        }
        _repo.GitletCommit("main commit");

        var mainHash = _state.GetCommitHash();

        // Act
        var exception = Should.Throw<GitletException>(() => _merge.GitletMerge("dev"));

        // Assert
        exception.Message.ShouldBe(ExceptionMessages.MergeConflict);

        //工作区写入了冲突内容
        File.ReadAllText(Path.Combine(_dir, "f.txt")).ShouldBe(expected);

        //冲突时仍然生成 merge commit,里面记录的是冲突内容
        var mergeCommit = _state.ReadCommit();
        mergeCommit.ParentHashes.ShouldBe([mainHash, devHash]);

        var blobHash = mergeCommit.Files["f.txt"];
        Encoding.UTF8.GetString(_store.Read("blob", blobHash)).ShouldBe(expected);
    }

    //merge commit 本身:消息、两个父、两边的文件都在
    [Theory]
    [InlineData("dev")]
    [InlineData("feature")]
    [InlineData("fix-1")]
    public void Merge_CreatesMergeCommitWithTwoParents(string givenBranch)
    {
        // Arrange
        _repo.Init();

        File.WriteAllText(Path.Combine(_dir, "base.txt"), "base");
        _repo.GitletAdd("base.txt");
        _repo.GitletCommit("base");

        _repo.GitletBranch(givenBranch);

        //给定分支
        _repo.GitletCheckoutBranch(givenBranch);
        File.WriteAllText(Path.Combine(_dir, "given.txt"), "given");
        _repo.GitletAdd("given.txt");
        _repo.GitletCommit("given commit");

        var givenHash = _state.GetCommitHash();

        //main
        _repo.GitletCheckoutBranch("main");
        File.WriteAllText(Path.Combine(_dir, "main.txt"), "main");
        _repo.GitletAdd("main.txt");
        _repo.GitletCommit("main commit");

        var mainHash = _state.GetCommitHash();

        // Act
        _merge.GitletMerge(givenBranch);

        // Assert
        var mergeCommit = _state.ReadCommit();

        mergeCommit.Message.ShouldBe($"Merged {givenBranch} into main.");
        mergeCommit.ParentHashes.ShouldBe([mainHash, givenHash]);
        mergeCommit.Files.Keys.ShouldBe(["base.txt", "given.txt", "main.txt"]);

        //当前分支还是 main,给定分支没动
        _state.CurrentBranch().ShouldBe("main");
        File.ReadAllText(_paths.BranchFile(givenBranch)).ShouldBe(givenHash);
    }

}
