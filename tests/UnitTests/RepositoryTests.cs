using System.Text.RegularExpressions;
using Shouldly;
using Xunit;

namespace Gitlet.Tests.UnitTests;

// Repository 直接读写磁盘,所以这里用每个测试独立的真实临时目录,不做 mock。
// 断言尽量只看稳定的东西(哈希是否变化、消息顺序、文件内容),
// 不依赖时间戳和本机时区。
public class RepositoryTests : IDisposable
{
    private readonly string _dir =
        Path.Combine(Path.GetTempPath(), "gitlet-repo-" + Guid.NewGuid());

    private readonly Repository _repo;

    public RepositoryTests()
    {
        Directory.CreateDirectory(_dir);
        _repo = new Repository(_dir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_dir))
        {
            Directory.Delete(_dir, recursive: true);
        }
    }

    // ---------- 辅助方法 ----------

    private string PathOf(string name) => Path.Combine(_dir, name);

    private void WriteFile(string name, string content) => File.WriteAllText(PathOf(name), content);

    private string ReadFile(string name) => File.ReadAllText(PathOf(name));

    private bool FileExists(string name) => File.Exists(PathOf(name));

    private void InitRepo() => _repo.Init();

    private void AddAndCommit(string name, string content, string message)
    {
        WriteFile(name, content);
        _repo.GitletAdd(name);
        _repo.GitletCommit(message);
    }

    // 从 log 里按"新到旧"取出所有 commit 哈希
    private static List<string> HashesIn(string log) =>
        Regex.Matches(log, @"commit\s+([0-9a-f]{40})").Select(m => m.Groups[1].Value).ToList();

    private string HeadHash() => HashesIn(_repo.GitletLog())[0];

    // status 的某个区块里列出的名字
    private string Section(string header)
    {
        var status = _repo.GitletStatus();
        var start = status.IndexOf(header, StringComparison.Ordinal) + header.Length;
        var end = status.IndexOf("\n\n", start, StringComparison.Ordinal);
        return status[start..end].Trim('\n');
    }

    private string StagedFiles() => Section("=== Staged Files ===");
    private string RemovedFiles() => Section("=== Removed Files ===");

    // ====================================================================
    // init
    // ====================================================================

    [Fact]
    public void Init_CreatesGitletLayout()
    {
        InitRepo();

        Directory.Exists(PathOf(".gitlet")).ShouldBeTrue();
        Directory.Exists(PathOf(".gitlet/refs/heads")).ShouldBeTrue();
        File.ReadAllText(PathOf(".gitlet/HEAD")).ShouldBe("ref: refs/heads/main");
        File.Exists(PathOf(".gitlet/refs/heads/main")).ShouldBeTrue();
    }

    [Fact]
    public void Init_CreatesInitialCommit()
    {
        InitRepo();

        var log = _repo.GitletLog();

        HashesIn(log).Count.ShouldBe(1);
        log.ShouldContain("initial commit");
    }

    [Fact]
    public void Init_InitialCommitIsIdenticalInEveryRepository()
    {
        // 初始 commit 的时间戳固定为 1970 年,所以不同仓库里哈希相同
        InitRepo();
        var otherDir = Path.Combine(Path.GetTempPath(), "gitlet-repo-" + Guid.NewGuid());
        try
        {
            Directory.CreateDirectory(otherDir);
            var other = new Repository(otherDir);
            other.Init();

            HashesIn(other.GitletLog()).ShouldBe(HashesIn(_repo.GitletLog()));
        }
        finally
        {
            Directory.Delete(otherDir, recursive: true);
        }
    }

    [Fact]
    public void Init_Twice_Throws()
    {
        InitRepo();

        Should.Throw<GitletException>(() => _repo.Init())
            .Message.ShouldBe(ExceptionMessages.AlreadyInitialized);
    }

    // ====================================================================
    // add
    // ====================================================================

    [Fact]
    public void Add_MissingFile_Throws()
    {
        InitRepo();

        Should.Throw<GitletException>(() => _repo.GitletAdd("nope.txt"))
            .Message.ShouldBe(ExceptionMessages.FileDoesNotExist);
    }

    [Fact]
    public void Add_StagesTheFile()
    {
        InitRepo();
        WriteFile("a.txt", "hello");

        _repo.GitletAdd("a.txt");

        StagedFiles().ShouldBe("a.txt");
    }

    [Theory]
    [InlineData("hello", "b6fc4c620b67d95f953a5c1c1230aaab5db5a1b0")]
    [InlineData("", "e69de29bb2d1d6434b8b29ae775ad8c2e48c5391")]
    public void Add_StoresFileContentAsBlob(string content, string expectedBlobHash)
    {
        InitRepo();
        WriteFile("a.txt", content);

        _repo.GitletAdd("a.txt");

        var store = new ObjectStore(new GitletPaths(_dir));
        store.Exists("blob", expectedBlobHash).ShouldBeTrue();
    }

    [Fact]
    public void Add_UnchangedSinceLastCommit_RemovesFromStaging()
    {
        InitRepo();
        AddAndCommit("a.txt", "v1", "first");

        _repo.GitletAdd("a.txt");

        StagedFiles().ShouldBeEmpty();
    }

    [Fact]
    public void Add_ModifiedSinceLastCommit_StagesAgain()
    {
        InitRepo();
        AddAndCommit("a.txt", "v1", "first");
        WriteFile("a.txt", "v2");

        _repo.GitletAdd("a.txt");

        StagedFiles().ShouldBe("a.txt");
    }

    [Fact]
    public void Add_FileMarkedForRemoval_CancelsTheRemoval()
    {
        InitRepo();
        AddAndCommit("a.txt", "v1", "first");
        _repo.GitletDelete("a.txt");
        WriteFile("a.txt", "v1"); // 用户把同样内容的文件放回来

        _repo.GitletAdd("a.txt");

        RemovedFiles().ShouldBeEmpty();
        StagedFiles().ShouldBeEmpty();
    }

    [Fact]
    public void Add_StagesFilesInSortedOrder()
    {
        InitRepo();
        WriteFile("b.txt", "b");
        WriteFile("a.txt", "a");

        _repo.GitletAdd("b.txt");
        _repo.GitletAdd("a.txt");

        StagedFiles().ShouldBe("a.txt\nb.txt");
    }

    // ====================================================================
    // commit
    // ====================================================================

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Commit_EmptyMessage_Throws(string? message)
    {
        InitRepo();
        WriteFile("a.txt", "x");
        _repo.GitletAdd("a.txt");

        Should.Throw<GitletException>(() => _repo.GitletCommit(message!))
            .Message.ShouldBe(ExceptionMessages.PleaseEnterCommitMessage);
    }

    [Fact]
    public void Commit_NothingStaged_Throws()
    {
        InitRepo();

        Should.Throw<GitletException>(() => _repo.GitletCommit("empty"))
            .Message.ShouldBe(ExceptionMessages.NoChangesAdded);
    }

    [Fact]
    public void Commit_ClearsStaging()
    {
        InitRepo();
        AddAndCommit("a.txt", "x", "first");

        StagedFiles().ShouldBeEmpty();
        RemovedFiles().ShouldBeEmpty();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(5)]
    public void Commit_AddsOneLogEntryPerCommit_NewestFirst(int count)
    {
        InitRepo();
        for (var i = 1; i <= count; i++)
        {
            AddAndCommit("a.txt", $"v{i}", $"message {i}");
        }

        var log = _repo.GitletLog();

        HashesIn(log).Count.ShouldBe(count + 1); // 加上 initial commit
        // 最新的 commit 在最前面(只有一个 commit 时两者是同一条)
        log.IndexOf($"message {count}", StringComparison.Ordinal)
            .ShouldBeLessThanOrEqualTo(log.IndexOf("message 1", StringComparison.Ordinal));
        log.TrimEnd().EndsWith("initial commit").ShouldBeTrue();
    }

    [Fact]
    public void Commit_KeepsFilesFromPreviousCommit()
    {
        InitRepo();
        AddAndCommit("a.txt", "a1", "add a");
        AddAndCommit("b.txt", "b1", "add b");
        File.Delete(PathOf("a.txt"));

        _repo.GitletCheckoutFile("a.txt"); // a.txt 仍然被当前 commit 跟踪

        ReadFile("a.txt").ShouldBe("a1");
    }

    [Fact]
    public void Commit_WithStagedRemoval_DropsFileFromCommit()
    {
        InitRepo();
        AddAndCommit("a.txt", "a1", "add a");
        _repo.GitletDelete("a.txt");

        _repo.GitletCommit("remove a");

        Should.Throw<GitletException>(() => _repo.GitletCheckoutFile("a.txt"))
            .Message.ShouldBe(ExceptionMessages.FileDoesNotExist);
    }

    [Fact]
    public void Commit_MovesOnlyTheCurrentBranch()
    {
        InitRepo();
        _repo.GitletBranch("dev");
        var before = HeadHash();

        AddAndCommit("a.txt", "x", "on main");

        File.ReadAllText(PathOf(".gitlet/refs/heads/dev")).Trim().ShouldBe(before);
        HeadHash().ShouldNotBe(before);
    }

    // ====================================================================
    // rm
    // ====================================================================

    [Fact]
    public void Rm_NeitherStagedNorTracked_Throws()
    {
        InitRepo();
        WriteFile("a.txt", "x");

        Should.Throw<GitletException>(() => _repo.GitletDelete("a.txt"))
            .Message.ShouldBe(ExceptionMessages.NoReasonToRemove);
        FileExists("a.txt").ShouldBeTrue();
    }

    [Fact]
    public void Rm_StagedButNotTracked_UnstagesAndKeepsFile()
    {
        InitRepo();
        WriteFile("a.txt", "x");
        _repo.GitletAdd("a.txt");

        _repo.GitletDelete("a.txt");

        StagedFiles().ShouldBeEmpty();
        RemovedFiles().ShouldBeEmpty();
        FileExists("a.txt").ShouldBeTrue();
    }

    [Fact]
    public void Rm_Tracked_StagesRemovalAndDeletesFile()
    {
        InitRepo();
        AddAndCommit("a.txt", "x", "add a");

        _repo.GitletDelete("a.txt");

        RemovedFiles().ShouldBe("a.txt");
        FileExists("a.txt").ShouldBeFalse();
    }

    [Fact]
    public void Rm_TrackedButAlreadyDeletedByUser_StillStagesRemoval()
    {
        InitRepo();
        AddAndCommit("a.txt", "x", "add a");
        File.Delete(PathOf("a.txt"));

        _repo.GitletDelete("a.txt");

        RemovedFiles().ShouldBe("a.txt");
    }

    [Fact]
    public void Rm_TrackedAndModifiedAndStaged_ClearsAdditionAndStagesRemoval()
    {
        InitRepo();
        AddAndCommit("a.txt", "v1", "add a");
        WriteFile("a.txt", "v2");
        _repo.GitletAdd("a.txt");

        _repo.GitletDelete("a.txt");

        StagedFiles().ShouldBeEmpty();
        RemovedFiles().ShouldBe("a.txt");
    }

    // ====================================================================
    // log / global-log / find
    // ====================================================================

    [Fact]
    public void GlobalLog_IncludesCommitsFromOtherBranches_LogDoesNot()
    {
        InitRepo();
        _repo.GitletBranch("dev");
        _repo.GitletCheckoutBranch("dev");
        AddAndCommit("a.txt", "x", "only on dev");
        _repo.GitletCheckoutBranch("main");

        _repo.GitletLog().ShouldNotContain("only on dev");
        _repo.GitletGlobalLog().ShouldContain("only on dev");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(3)]
    public void GlobalLog_HasOneEntryPerCommit(int extraCommits)
    {
        InitRepo();
        for (var i = 0; i < extraCommits; i++)
        {
            AddAndCommit("a.txt", $"v{i}", $"c{i}");
        }

        HashesIn(_repo.GitletGlobalLog()).Count.ShouldBe(extraCommits + 1);
    }

    [Fact]
    public void Find_ReturnsHashOfMatchingCommit()
    {
        InitRepo();
        AddAndCommit("a.txt", "x", "target");
        var expected = HeadHash();

        _repo.GitletFind("target").ShouldBe(expected + "\n");
    }

    [Fact]
    public void Find_SameMessageOnSeveralCommits_ReturnsAllOfThem()
    {
        InitRepo();
        AddAndCommit("a.txt", "1", "same");
        AddAndCommit("a.txt", "2", "same");

        var lines = _repo.GitletFind("same").Split('\n', StringSplitOptions.RemoveEmptyEntries);

        lines.Length.ShouldBe(2);
        lines.Distinct().Count().ShouldBe(2);
    }

    [Theory]
    [InlineData("nothing like this")]
    [InlineData("initial")]        // 必须完全相同,前缀不算
    [InlineData("Initial Commit")] // 区分大小写
    public void Find_NoExactMatch_Throws(string message)
    {
        InitRepo();

        Should.Throw<GitletException>(() => _repo.GitletFind(message))
            .Message.ShouldBe(ExceptionMessages.NoCommitWithMessage);
    }

    // ====================================================================
    // branch / rm-branch
    // ====================================================================

    [Fact]
    public void Branch_CreatesBranchWithoutSwitching()
    {
        InitRepo();

        _repo.GitletBranch("dev");

        var status = _repo.GitletStatus();
        status.ShouldContain("*main\n");
        status.ShouldContain("dev\n");
        status.ShouldNotContain("*dev");
    }

    [Fact]
    public void Branch_PointsAtCurrentCommit()
    {
        InitRepo();
        AddAndCommit("a.txt", "x", "first");

        _repo.GitletBranch("dev");

        File.ReadAllText(PathOf(".gitlet/refs/heads/dev")).Trim().ShouldBe(HeadHash());
    }

    [Theory]
    [InlineData("main")]
    [InlineData("dev")]
    public void Branch_ExistingName_Throws(string name)
    {
        InitRepo();
        _repo.GitletBranch("dev");

        Should.Throw<GitletException>(() => _repo.GitletBranch(name))
            .Message.ShouldBe(ExceptionMessages.BranchAlreadyExists);
    }

    [Fact]
    public void RmBranch_RemovesTheBranchButKeepsItsCommits()
    {
        InitRepo();
        _repo.GitletBranch("dev");
        _repo.GitletCheckoutBranch("dev");
        AddAndCommit("a.txt", "x", "only on dev");
        _repo.GitletCheckoutBranch("main");

        _repo.GitletRmBranch("dev");

        _repo.GitletStatus().ShouldNotContain("dev");
        _repo.GitletGlobalLog().ShouldContain("only on dev");
    }

    [Fact]
    public void RmBranch_Unknown_Throws()
    {
        InitRepo();

        Should.Throw<GitletException>(() => _repo.GitletRmBranch("ghost"))
            .Message.ShouldBe(ExceptionMessages.BranchNoExists);
    }

    [Fact]
    public void RmBranch_CurrentBranch_Throws()
    {
        InitRepo();

        Should.Throw<GitletException>(() => _repo.GitletRmBranch("main"))
            .Message.ShouldBe(ExceptionMessages.DeleteCurrentBranchForbiden);
    }

    // ====================================================================
    // status
    // ====================================================================

    [Fact]
    public void Status_FreshRepository_HasExactLayout()
    {
        InitRepo();

        _repo.GitletStatus().ShouldBe(
            "=== Branches ===\n*main\n\n" +
            "=== Staged Files ===\n\n" +
            "=== Removed Files ===\n\n" +
            "=== Modifications Not Staged For Commit ===\n\n" +
            "=== Untracked Files ===\n\n");
    }

    [Fact]
    public void Status_BranchesAreSortedAndCurrentOneIsMarked()
    {
        InitRepo();
        _repo.GitletBranch("zeta");
        _repo.GitletBranch("alpha");

        _repo.GitletStatus().ShouldStartWith("=== Branches ===\nalpha\n*main\nzeta\n\n");
    }

    [Fact]
    public void Status_ShowsStagedAndRemovedFiles()
    {
        InitRepo();
        AddAndCommit("old.txt", "x", "add old");
        _repo.GitletDelete("old.txt");
        WriteFile("new.txt", "y");
        _repo.GitletAdd("new.txt");

        StagedFiles().ShouldBe("new.txt");
        RemovedFiles().ShouldBe("old.txt");
    }

    // ====================================================================
    // checkout -- <file>
    // ====================================================================

    [Fact]
    public void CheckoutFile_RestoresCommittedVersionAndDoesNotStage()
    {
        InitRepo();
        AddAndCommit("a.txt", "committed", "first");
        WriteFile("a.txt", "scribbled");

        _repo.GitletCheckoutFile("a.txt");

        ReadFile("a.txt").ShouldBe("committed");
        StagedFiles().ShouldBeEmpty();
    }

    [Fact]
    public void CheckoutFile_RecreatesDeletedFile()
    {
        InitRepo();
        AddAndCommit("a.txt", "committed", "first");
        File.Delete(PathOf("a.txt"));

        _repo.GitletCheckoutFile("a.txt");

        ReadFile("a.txt").ShouldBe("committed");
    }

    [Fact]
    public void CheckoutFile_NotInCommit_Throws()
    {
        InitRepo();

        Should.Throw<GitletException>(() => _repo.GitletCheckoutFile("a.txt"))
            .Message.ShouldBe(ExceptionMessages.FileDoesNotExist);
    }

    // ====================================================================
    // checkout <commit id> -- <file>
    // ====================================================================

    [Theory]
    [InlineData(40)] // 完整哈希
    [InlineData(8)]  // 缩写
    public void CheckoutCommitFile_RestoresVersionFromThatCommit(int idLength)
    {
        InitRepo();
        AddAndCommit("a.txt", "v1", "first");
        var firstId = HeadHash()[..idLength];
        AddAndCommit("a.txt", "v2", "second");

        _repo.GitletCheckoutCommitFile(firstId, "a.txt");

        ReadFile("a.txt").ShouldBe("v1");
        StagedFiles().ShouldBeEmpty();
    }

    [Fact]
    public void CheckoutCommitFile_UnknownCommit_Throws()
    {
        InitRepo();

        Should.Throw<GitletException>(() => _repo.GitletCheckoutCommitFile("ffffffff", "a.txt"))
            .Message.ShouldBe(ExceptionMessages.CommitDoesNotExist);
    }

    [Fact]
    public void CheckoutCommitFile_FileNotInThatCommit_Throws()
    {
        InitRepo();
        var initialId = HeadHash();
        AddAndCommit("a.txt", "v1", "first");

        Should.Throw<GitletException>(() => _repo.GitletCheckoutCommitFile(initialId, "a.txt"))
            .Message.ShouldBe(ExceptionMessages.FileDoesNotExist);
    }

    // ====================================================================
    // checkout <branch>
    // ====================================================================

    [Fact]
    public void CheckoutBranch_Unknown_Throws()
    {
        InitRepo();

        Should.Throw<GitletException>(() => _repo.GitletCheckoutBranch("ghost"))
            .Message.ShouldBe(ExceptionMessages.BranchNoExists);
    }

    [Fact]
    public void CheckoutBranch_CurrentBranch_Throws()
    {
        InitRepo();

        Should.Throw<GitletException>(() => _repo.GitletCheckoutBranch("main"))
            .Message.ShouldBe(ExceptionMessages.ALreadyOnTheBranch);
    }

    [Fact]
    public void CheckoutBranch_SwitchesCurrentBranch()
    {
        InitRepo();
        _repo.GitletBranch("dev");

        _repo.GitletCheckoutBranch("dev");

        _repo.GitletStatus().ShouldContain("*dev\n");
        File.ReadAllText(PathOf(".gitlet/HEAD")).ShouldBe("ref: refs/heads/dev");
    }

    [Fact]
    public void CheckoutBranch_SwapsWorkingDirectoryFiles()
    {
        InitRepo();
        AddAndCommit("shared.txt", "main version", "on main");
        _repo.GitletBranch("dev");
        _repo.GitletCheckoutBranch("dev");
        AddAndCommit("shared.txt", "dev version", "dev edit");
        AddAndCommit("dev-only.txt", "d", "dev file");

        _repo.GitletCheckoutBranch("main");

        ReadFile("shared.txt").ShouldBe("main version");
        FileExists("dev-only.txt").ShouldBeFalse(); // 目标分支没有的文件被删掉

        _repo.GitletCheckoutBranch("dev");

        ReadFile("shared.txt").ShouldBe("dev version");
        ReadFile("dev-only.txt").ShouldBe("d");
    }

    [Fact]
    public void CheckoutBranch_ClearsStaging()
    {
        InitRepo();
        _repo.GitletBranch("dev");
        WriteFile("a.txt", "x");
        _repo.GitletAdd("a.txt");

        _repo.GitletCheckoutBranch("dev");

        StagedFiles().ShouldBeEmpty();
    }

    [Fact]
    public void CheckoutBranch_UntrackedFileWouldBeOverwritten_Throws()
    {
        InitRepo();
        _repo.GitletBranch("dev");
        _repo.GitletCheckoutBranch("dev");
        AddAndCommit("a.txt", "tracked on dev", "dev file");
        _repo.GitletCheckoutBranch("main");
        WriteFile("a.txt", "untracked on main");

        Should.Throw<GitletException>(() => _repo.GitletCheckoutBranch("dev"))
            .Message.ShouldBe(ExceptionMessages.UntrackedFileInTheWay);
        ReadFile("a.txt").ShouldBe("untracked on main"); // 没有被覆盖
    }

    // ====================================================================
    // reset
    // ====================================================================

    [Theory]
    [InlineData(40)]
    [InlineData(8)]
    public void Reset_MovesBranchAndWorkingDirectoryBackToThatCommit(int idLength)
    {
        InitRepo();
        AddAndCommit("a.txt", "v1", "first");
        var firstId = HeadHash()[..idLength];
        AddAndCommit("a.txt", "v2", "second");
        AddAndCommit("b.txt", "b", "third");

        _repo.GitletReset(firstId);

        ReadFile("a.txt").ShouldBe("v1");
        FileExists("b.txt").ShouldBeFalse();
        var log = _repo.GitletLog();
        HashesIn(log).Count.ShouldBe(2); // initial + first
        log.ShouldNotContain("second");
        log.ShouldNotContain("third");
    }

    [Fact]
    public void Reset_ClearsStaging()
    {
        InitRepo();
        AddAndCommit("a.txt", "v1", "first");
        var firstId = HeadHash();
        WriteFile("a.txt", "v2");
        _repo.GitletAdd("a.txt");

        _repo.GitletReset(firstId);

        StagedFiles().ShouldBeEmpty();
    }

    [Fact]
    public void Reset_KeepsLaterCommitsInGlobalLog()
    {
        InitRepo();
        AddAndCommit("a.txt", "v1", "first");
        var firstId = HeadHash();
        AddAndCommit("a.txt", "v2", "second");

        _repo.GitletReset(firstId);

        _repo.GitletGlobalLog().ShouldContain("second");
    }

    [Fact]
    public void Reset_UnknownCommit_Throws()
    {
        InitRepo();

        Should.Throw<GitletException>(() => _repo.GitletReset("ffffffff"))
            .Message.ShouldBe(ExceptionMessages.CommitDoesNotExist);
    }

    [Fact]
    public void Reset_UntrackedFileWouldBeOverwritten_Throws()
    {
        InitRepo();
        AddAndCommit("a.txt", "v1", "first");
        var firstId = HeadHash();
        _repo.GitletDelete("a.txt");
        _repo.GitletCommit("remove a");
        WriteFile("a.txt", "untracked now");

        Should.Throw<GitletException>(() => _repo.GitletReset(firstId))
            .Message.ShouldBe(ExceptionMessages.UntrackedFileInTheWay);
        ReadFile("a.txt").ShouldBe("untracked now");
    }
}
