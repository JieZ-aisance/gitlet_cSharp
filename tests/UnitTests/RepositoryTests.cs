using System.Text.RegularExpressions;
using Shouldly;

namespace Gitlet.Tests.UnitTests;


public class RepositoryTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "gitlet-repo-" + Guid.NewGuid());

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

    // helpers
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


    private static List<string> HashesIn(string log) =>
        Regex.Matches(log, @"commit\s+([0-9a-f]{40})").Select(m => m.Groups[1].Value).ToList();

    private string HeadHash() => HashesIn(_repo.GitletLog())[0];


    private string Section(string header)
    {
        var status = _repo.GitletStatus();
        var start = status.IndexOf(header, StringComparison.Ordinal) + header.Length;
        var end = status.IndexOf("\n\n", start, StringComparison.Ordinal);
        return status[start..end].Trim('\n');
    }

    private string StagedFiles() => Section("=== Staged Files ===");
    private string RemovedFiles() => Section("=== Removed Files ===");

    
    [Fact]
    public void Init_CreatesGitletLayout()
    {
        // Act
        InitRepo();

        // Assert
        Directory.Exists(PathOf(".gitlet")).ShouldBeTrue();
        
        Directory.Exists(PathOf(".gitlet/refs/heads")).ShouldBeTrue();
        
        File.ReadAllText(PathOf(".gitlet/HEAD")).ShouldBe("ref: refs/heads/main");
        
        File.Exists(PathOf(".gitlet/refs/heads/main")).ShouldBeTrue();
    }

    [Fact]
    public void Init_CreatesInitialCommit()
    {
        // Act
        InitRepo();

        // Assert
        var log = _repo.GitletLog();

        HashesIn(log).Count.ShouldBe(1);
        
        log.ShouldContain("initial commit");
    }

    [Fact]
    public void Init_InitialCommitIsIdenticalInEveryRepository()
    {

        // Arrange
        InitRepo();
        
        var otherDir = Path.Combine(Path.GetTempPath(), "gitlet-repo-" + Guid.NewGuid());
        
        try
        {
            Directory.CreateDirectory(otherDir);
            
            var other = new Repository(otherDir);
            
            // Act
            other.Init();

            // Assert
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
        // Arrange
        InitRepo();

        // Act & Assert
        Should.Throw<GitletException>(() => _repo.Init()).Message.ShouldBe(ExceptionMessages.AlreadyInitialized);
    }
    

    [Fact]
    public void Add_MissingFile_Throws()
    {
        // Arrange
        InitRepo();

        // Act & Assert
        Should.Throw<GitletException>(() => _repo.GitletAdd("nope.txt")).Message.ShouldBe(ExceptionMessages.FileDoesNotExist);
    }

    [Fact]
    public void Add_StagesTheFile()
    {
        // Arrange
        InitRepo();
        
        WriteFile("a.txt", "hello");

        // Act
        _repo.GitletAdd("a.txt");

        // Assert
        StagedFiles().ShouldBe("a.txt");
    }

    
    
    [Theory]
    [InlineData("hello", "b6fc4c620b67d95f953a5c1c1230aaab5db5a1b0")]
    [InlineData("", "e69de29bb2d1d6434b8b29ae775ad8c2e48c5391")]
    public void Add_StoresFileContentAsBlob(string content, string expectedBlobHash)
    {
        // Arrange
        InitRepo();
        
        WriteFile("a.txt", content);

        // Act
        _repo.GitletAdd("a.txt");

        // Assert
        var store = new ObjectStore(new GitletPaths(_dir));
        
        store.Exists("blob", expectedBlobHash).ShouldBeTrue();
        
    }

    [Fact]
    public void Add_UnchangedSinceLastCommit_RemovesFromStaging()
    {
        // Arrange
        InitRepo();
        
        AddAndCommit("a.txt", "v1", "first");

        // Act
        _repo.GitletAdd("a.txt");

        // Assert
        StagedFiles().ShouldBeEmpty();
        
    }

    [Fact]
    public void Add_ModifiedSinceLastCommit_StagesAgain()
    {
        // Arrange
        InitRepo();
        
        AddAndCommit("a.txt", "v1", "first");
        
        WriteFile("a.txt", "v2");

        // Act
        _repo.GitletAdd("a.txt");

        // Assert
        StagedFiles().ShouldBe("a.txt");
    }

    [Fact]
    public void Add_FileMarkedForRemoval_CancelsTheRemoval()
    {
        // Arrange
        InitRepo();
        AddAndCommit("a.txt", "v1", "first");
        
        _repo.GitletDelete("a.txt");
        
        WriteFile("a.txt", "v1"); 

        // Act
        _repo.GitletAdd("a.txt");

        // Assert
        RemovedFiles().ShouldBeEmpty();
        
        StagedFiles().ShouldBeEmpty();
    }

    [Fact]
    public void Add_StagesFilesInSortedOrder()
    {
        // Arrange
        InitRepo();
        
        WriteFile("b.txt", "b");
        
        WriteFile("a.txt", "a");

        // Act
        _repo.GitletAdd("b.txt");
        
        _repo.GitletAdd("a.txt");

        // Assert
        StagedFiles().ShouldBe("a.txt\nb.txt");
        
    }



    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Commit_EmptyMessage_Throws(string? message)
    {
        // Arrange
        InitRepo();
        
        WriteFile("a.txt", "x");
        
        _repo.GitletAdd("a.txt");

        // Act & Assert
        Should.Throw<GitletException>(() => _repo.GitletCommit(message!)) .Message.ShouldBe(ExceptionMessages.PleaseEnterCommitMessage);
        
    }

    [Fact]
    public void Commit_NothingStaged_Throws()
    {
        // Arrange
        InitRepo();

        // Act & Assert
        Should.Throw<GitletException>(() => _repo.GitletCommit("empty")).Message.ShouldBe(ExceptionMessages.NoChangesAdded);
        
    }

    [Fact]
    public void Commit_ClearsStaging()
    {
        // Arrange
        InitRepo();
        
        // Act
        AddAndCommit("a.txt", "x", "first");

        // Assert
        StagedFiles().ShouldBeEmpty();
        
        RemovedFiles().ShouldBeEmpty();
    }

    
    
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(5)]
    public void Commit_AddsOneLogEntryPerCommit_NewestFirst(int count)
    {
        // Arrange
        InitRepo();
        
        // Act
        for (var i = 1; i <= count; i++)
        {
            AddAndCommit("a.txt", $"v{i}", $"message {i}");
        }

        // Assert
        var log = _repo.GitletLog();

        HashesIn(log).Count.ShouldBe(count + 1); 

        log.IndexOf($"message {count}", StringComparison.Ordinal)
            .ShouldBeLessThanOrEqualTo(log.IndexOf("message 1", StringComparison.Ordinal));
        
        log.TrimEnd().EndsWith("initial commit").ShouldBeTrue();
    }

    [Fact]
    public void Commit_KeepsFilesFromPreviousCommit()
    {
        // Arrange
        InitRepo();
        
        AddAndCommit("a.txt", "a1", "add a");
        
        AddAndCommit("b.txt", "b1", "add b");
        
        File.Delete(PathOf("a.txt"));

        // Act
        _repo.GitletCheckoutFile("a.txt"); 

        // Assert
        ReadFile("a.txt").ShouldBe("a1");
        
    }

    [Fact]
    public void Commit_WithStagedRemoval_DropsFileFromCommit()
    {
        // Arrange
        InitRepo();
        
        AddAndCommit("a.txt", "a1", "add a");
        
        _repo.GitletDelete("a.txt");

        // Act
        _repo.GitletCommit("remove a");

        // Assert
        Should.Throw<GitletException>(() => _repo.GitletCheckoutFile("a.txt"))
            .Message.ShouldBe(ExceptionMessages.FileDoesNotExist);
        
    }

    [Fact]
    public void Commit_MovesOnlyTheCurrentBranch()
    {
        // Arrange
        InitRepo();
        
        _repo.GitletBranch("dev");
        
        var before = HeadHash();

        // Act
        AddAndCommit("a.txt", "x", "on main");

        // Assert
        File.ReadAllText(PathOf(".gitlet/refs/heads/dev")).Trim().ShouldBe(before);
        
        HeadHash().ShouldNotBe(before);
    }



    [Fact]
    public void Rm_NeitherStagedNorTracked_Throws()
    {
        // Arrange
        InitRepo();
        
        WriteFile("a.txt", "x");

        // Act & Assert
        Should.Throw<GitletException>(() => _repo.GitletDelete("a.txt"))
            .Message.ShouldBe(ExceptionMessages.NoReasonToRemove);
        
        FileExists("a.txt").ShouldBeTrue();
        
    }

    
    
    [Fact]
    public void Rm_StagedButNotTracked_UnstagesAndKeepsFile()
    {
        // Arrange
        InitRepo();
        
        WriteFile("a.txt", "x");
        
        _repo.GitletAdd("a.txt");

        // Act
        _repo.GitletDelete("a.txt");

        // Assert
        StagedFiles().ShouldBeEmpty();
        
        RemovedFiles().ShouldBeEmpty();
        
        FileExists("a.txt").ShouldBeTrue();
        
    }

    
    [Fact]
    public void Rm_Tracked_StagesRemovalAndDeletesFile()
    {
        // Arrange
        InitRepo();
        
        AddAndCommit("a.txt", "x", "add a");

        // Act
        _repo.GitletDelete("a.txt");

        // Assert
        RemovedFiles().ShouldBe("a.txt");
        
        FileExists("a.txt").ShouldBeFalse();
        
    }

    [Fact]
    public void Rm_TrackedButAlreadyDeletedByUser_StillStagesRemoval()
    {
        // Arrange
        InitRepo();
        
        AddAndCommit("a.txt", "x", "add a");
        
        File.Delete(PathOf("a.txt"));

        // Act
        _repo.GitletDelete("a.txt");

        // Assert
        RemovedFiles().ShouldBe("a.txt");
        
    }
    
    
    [Fact]
    public void Rm_TrackedAndModifiedAndStaged_ClearsAdditionAndStagesRemoval()
    {
        // Arrange
        InitRepo();
        
        AddAndCommit("a.txt", "v1", "add a");
        
        WriteFile("a.txt", "v2");
        
        _repo.GitletAdd("a.txt");

        // Act
        _repo.GitletDelete("a.txt");

        // Assert
        StagedFiles().ShouldBeEmpty();
        
        RemovedFiles().ShouldBe("a.txt");
    }
    

    [Fact]
    public void GlobalLog_IncludesCommitsFromOtherBranches_LogDoesNot()
    {
        // Arrange
        InitRepo();
        
        _repo.GitletBranch("dev");
        
        _repo.GitletCheckoutBranch("dev");
        
        AddAndCommit("a.txt", "x", "only on dev");
        
        _repo.GitletCheckoutBranch("main");

        // Act & Assert
        _repo.GitletLog().ShouldNotContain("only on dev");
        
        _repo.GitletGlobalLog().ShouldContain("only on dev");
        
    }

    
    
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(3)]
    public void GlobalLog_HasOneEntryPerCommit(int extraCommits)
    {
        // Arrange
        InitRepo();
        
        for (var i = 0; i < extraCommits; i++)
        {
            
            AddAndCommit("a.txt", $"v{i}", $"c{i}");
            
        }

        // Act & Assert
        HashesIn(_repo.GitletGlobalLog()).Count.ShouldBe(extraCommits + 1);
    }

    
    [Fact]
    public void Find_ReturnsHashOfMatchingCommit()
    {
        // Arrange
        InitRepo();
        
        AddAndCommit("a.txt", "x", "target");
        
        var expected = HeadHash();

        // Act & Assert
        _repo.GitletFind("target").ShouldBe(expected + "\n");
    }

    [Fact]
    public void Find_SameMessageOnSeveralCommits_ReturnsAllOfThem()
    {
        // Arrange
        InitRepo();
        
        AddAndCommit("a.txt", "1", "same");
        
        AddAndCommit("a.txt", "2", "same");

        // Act
        var lines = _repo.GitletFind("same").Split('\n', StringSplitOptions.RemoveEmptyEntries);

        // Assert
        lines.Length.ShouldBe(2);
        
        lines.Distinct().Count().ShouldBe(2);
    }

    [Theory]
    [InlineData("nothing like this")]
    [InlineData("initial")]        
    public void Find_NoExactMatch_Throws(string message)
    {
        // Arrange
        InitRepo();

        // Act & Assert
        Should.Throw<GitletException>(() => _repo.GitletFind(message))
            .Message.ShouldBe(ExceptionMessages.NoCommitWithMessage);
    }

    

    [Fact]
    public void Branch_CreatesBranchWithoutSwitching()
    {
        // Arrange
        InitRepo();

        // Act
        _repo.GitletBranch("dev");

        // Assert
        var status = _repo.GitletStatus();
        
        status.ShouldContain("*main\n");
        
        status.ShouldContain("dev\n");
        
        status.ShouldNotContain("*dev");
    }

    [Fact]
    public void Branch_PointsAtCurrentCommit()
    {
        // Arrange
        InitRepo();
        
        AddAndCommit("a.txt", "x", "first");

        // Act
        _repo.GitletBranch("dev");

        // Assert
        File.ReadAllText(PathOf(".gitlet/refs/heads/dev")).Trim().ShouldBe(HeadHash());
    }

    [Theory]
    [InlineData("main")]
    [InlineData("dev")]
    public void Branch_ExistingName_Throws(string name)
    {
        
        // Arrange
        InitRepo();
        
        _repo.GitletBranch("dev");

        // Act & Assert
        Should.Throw<GitletException>(() => _repo.GitletBranch(name))
            .Message.ShouldBe(ExceptionMessages.BranchAlreadyExists);
    }

    
    
    [Fact]
    public void RmBranch_RemovesTheBranchButKeepsItsCommits()
    {
        // Arrange
        InitRepo();
        
        _repo.GitletBranch("dev");
        
        _repo.GitletCheckoutBranch("dev");
        
        AddAndCommit("a.txt", "x", "only on dev");
        
        _repo.GitletCheckoutBranch("main");

        // Act
        _repo.GitletRmBranch("dev");

        // Assert
        _repo.GitletStatus().ShouldNotContain("dev");
        
        _repo.GitletGlobalLog().ShouldContain("only on dev");
        
    }

    
    
    [Fact]
    public void RmBranch_Unknown_Throws()
    {
        // Arrange
        InitRepo();

        // Act & Assert
        Should.Throw<GitletException>(() => _repo.GitletRmBranch("ghost"))
            .Message.ShouldBe(ExceptionMessages.BranchNoExists);
        
    }

    
    
    [Fact]
    public void RmBranch_CurrentBranch_Throws()
    {
        // Arrange
        InitRepo();

        // Act & Assert
        Should.Throw<GitletException>(() => _repo.GitletRmBranch("main"))
            .Message.ShouldBe(ExceptionMessages.DeleteCurrentBranchForbiden);
        
    }

    
    [Fact]
    public void Status_FreshRepository_HasExactLayout()
    {
        // Arrange
        InitRepo();

        // Act & Assert
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
        // Arrange
        InitRepo();
        
        _repo.GitletBranch("zeta");
        
        _repo.GitletBranch("alpha");

        // Act & Assert
        _repo.GitletStatus().ShouldStartWith("=== Branches ===\nalpha\n*main\nzeta\n\n");
        
    }

    
    
    [Fact]
    public void Status_ShowsStagedAndRemovedFiles()
    {
        // Arrange
        InitRepo();
        
        AddAndCommit("old.txt", "x", "add old");
        
        _repo.GitletDelete("old.txt");
        
        WriteFile("new.txt", "y");
        
        _repo.GitletAdd("new.txt");

        // Act & Assert
        StagedFiles().ShouldBe("new.txt");
        
        RemovedFiles().ShouldBe("old.txt");
        
    }



    [Fact]
    public void CheckoutFile_RestoresCommittedVersionAndDoesNotStage()
    {
        // Arrange
        InitRepo();
        
        AddAndCommit("a.txt", "committed", "first");
        
        WriteFile("a.txt", "scribbled");

        // Act
        _repo.GitletCheckoutFile("a.txt");

        // Assert
        ReadFile("a.txt").ShouldBe("committed");
        
        StagedFiles().ShouldBeEmpty();
        
    }

    [Fact]
    public void CheckoutFile_RecreatesDeletedFile()
    {
        // Arrange
        InitRepo();
        
        AddAndCommit("a.txt", "committed", "first");
        
        File.Delete(PathOf("a.txt"));

        // Act
        _repo.GitletCheckoutFile("a.txt");

        // Assert
        ReadFile("a.txt").ShouldBe("committed");
        
    }

    [Fact]
    public void CheckoutFile_NotInCommit_Throws()
    {
        // Arrange
        InitRepo();

        // Act & Assert
        Should.Throw<GitletException>(() => _repo.GitletCheckoutFile("a.txt"))
            .Message.ShouldBe(ExceptionMessages.FileDoesNotExist);
    }


    

    [Theory]
    [InlineData(40)] 
    [InlineData(8)]  
    public void CheckoutCommitFile_RestoresVersionFromThatCommit(int idLength)
    {
        // Arrange
        InitRepo();
        
        AddAndCommit("a.txt", "v1", "first");
        
        var firstId = HeadHash()[..idLength];
        
        AddAndCommit("a.txt", "v2", "second");

        // Act
        _repo.GitletCheckoutCommitFile(firstId, "a.txt");

        // Assert
        ReadFile("a.txt").ShouldBe("v1");
        
        StagedFiles().ShouldBeEmpty();
    }

    
    [Fact]
    public void CheckoutCommitFile_UnknownCommit_Throws()
    {
        // Arrange
        InitRepo();

        // Act & Assert
        Should.Throw<GitletException>(() => _repo.GitletCheckoutCommitFile("ffffffff", "a.txt"))
            .Message.ShouldBe(ExceptionMessages.CommitDoesNotExist);
    }

    
    [Fact]
    public void CheckoutCommitFile_FileNotInThatCommit_Throws()
    {
        // Arrange
        InitRepo();
        
        var initialId = HeadHash();
        
        AddAndCommit("a.txt", "v1", "first");

        // Act & Assert
        Should.Throw<GitletException>(() => _repo.GitletCheckoutCommitFile(initialId, "a.txt"))
            .Message.ShouldBe(ExceptionMessages.FileDoesNotExist);
    }

    

    [Fact]
    public void CheckoutBranch_Unknown_Throws()
    {
        // Arrange
        InitRepo();

        // Act & Assert
        Should.Throw<GitletException>(() => _repo.GitletCheckoutBranch("ghost"))
            .Message.ShouldBe(ExceptionMessages.BranchNoExists);
    }

    [Fact]
    public void CheckoutBranch_CurrentBranch_Throws()
    {
        // Arrange
        InitRepo();

        // Act & Assert
        Should.Throw<GitletException>(() => _repo.GitletCheckoutBranch("main"))
            .Message.ShouldBe(ExceptionMessages.ALreadyOnTheBranch);
    }

    [Fact]
    public void CheckoutBranch_SwitchesCurrentBranch()
    {
        // Arrange
        InitRepo();
        
        _repo.GitletBranch("dev");

        // Act
        _repo.GitletCheckoutBranch("dev");

        // Assert
        _repo.GitletStatus().ShouldContain("*dev\n");
        
        File.ReadAllText(PathOf(".gitlet/HEAD")).ShouldBe("ref: refs/heads/dev");
        
    }

    
    [Fact]
    public void CheckoutBranch_SwapsWorkingDirectoryFiles()
    {
        // Arrange
        InitRepo();
        
        AddAndCommit("shared.txt", "main version", "on main");
        
        _repo.GitletBranch("dev");
        
        _repo.GitletCheckoutBranch("dev");
        
        AddAndCommit("shared.txt", "dev version", "dev edit");
        
        AddAndCommit("dev-only.txt", "d", "dev file");

        // Act
        _repo.GitletCheckoutBranch("main");

        // Assert
        ReadFile("shared.txt").ShouldBe("main version");
        
        FileExists("dev-only.txt").ShouldBeFalse(); 

        // Act
        _repo.GitletCheckoutBranch("dev");

        // Assert
        ReadFile("shared.txt").ShouldBe("dev version");
        
        ReadFile("dev-only.txt").ShouldBe("d");
        
    }

    
    
    [Fact]
    public void CheckoutBranch_ClearsStaging()
    {
        // Arrange
        InitRepo();
        
        _repo.GitletBranch("dev");
        
        WriteFile("a.txt", "x");
        
        _repo.GitletAdd("a.txt");

        // Act
        _repo.GitletCheckoutBranch("dev");

        // Assert
        StagedFiles().ShouldBeEmpty();
        
    }

    
    
    [Fact]
    public void CheckoutBranch_UntrackedFileWouldBeOverwritten_Throws()
    {
        // Arrange
        InitRepo();
        
        _repo.GitletBranch("dev");
        
        _repo.GitletCheckoutBranch("dev");
        
        AddAndCommit("a.txt", "tracked on dev", "dev file");
        
        _repo.GitletCheckoutBranch("main");
        
        WriteFile("a.txt", "untracked on main");

        // Act & Assert
        Should.Throw<GitletException>(() => _repo.GitletCheckoutBranch("dev"))
            .Message.ShouldBe(ExceptionMessages.UntrackedFileInTheWay);
        
        ReadFile("a.txt").ShouldBe("untracked on main"); 
    }

    

    [Theory]
    [InlineData(40)]
    [InlineData(8)]
    public void Reset_MovesBranchAndWorkingDirectoryBackToThatCommit(int idLength)
    {
        // Arrange
        InitRepo();
        
        AddAndCommit("a.txt", "v1", "first");
        
        var firstId = HeadHash()[..idLength];
        
        AddAndCommit("a.txt", "v2", "second");
        
        AddAndCommit("b.txt", "b", "third");

        // Act
        _repo.GitletReset(firstId);

        // Assert
        ReadFile("a.txt").ShouldBe("v1");
        
        FileExists("b.txt").ShouldBeFalse();
        
        var log = _repo.GitletLog();
        
        HashesIn(log).Count.ShouldBe(2); 
        
        log.ShouldNotContain("second");
        
        log.ShouldNotContain("third");
        
    }

    
    
    [Fact]
    public void Reset_ClearsStaging()
    {
        // Arrange
        InitRepo();
        
        AddAndCommit("a.txt", "v1", "first");
        
        var firstId = HeadHash();
        
        WriteFile("a.txt", "v2");
        
        _repo.GitletAdd("a.txt");

        // Act
        _repo.GitletReset(firstId);

        // Assert
        StagedFiles().ShouldBeEmpty();
        
    }

    
    
    [Fact]
    public void Reset_KeepsLaterCommitsInGlobalLog()
    {
        // Arrange
        InitRepo();
        
        AddAndCommit("a.txt", "v1", "first");
        
        var firstId = HeadHash();
        
        AddAndCommit("a.txt", "v2", "second");

        // Act
        _repo.GitletReset(firstId);

        // Assert
        _repo.GitletGlobalLog().ShouldContain("second");
        
    }

    
    
    [Fact]
    public void Reset_UnknownCommit_Throws()
    {
        // Arrange
        InitRepo();

        // Act & Assert
        Should.Throw<GitletException>(() => _repo.GitletReset("ffffffff"))
            .Message.ShouldBe(ExceptionMessages.CommitDoesNotExist);
    }

    [Fact]
    public void Reset_UntrackedFileWouldBeOverwritten_Throws()
    {
        // Arrange
        InitRepo();
        
        AddAndCommit("a.txt", "v1", "first");
        
        var firstId = HeadHash();
        
        _repo.GitletDelete("a.txt");
        
        _repo.GitletCommit("remove a");
        
        WriteFile("a.txt", "untracked now");

        // Act & Assert
        Should.Throw<GitletException>(() => _repo.GitletReset(firstId))
            .Message.ShouldBe(ExceptionMessages.UntrackedFileInTheWay);
        
        ReadFile("a.txt").ShouldBe("untracked now");
        
    }
    
}
