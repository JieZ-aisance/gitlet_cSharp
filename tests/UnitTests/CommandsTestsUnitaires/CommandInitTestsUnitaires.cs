using Gitlet.Commands;
using Shouldly;

namespace Gitlet.Tests.UnitTests;

public class CommandInitTestsUnitaires : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "gitlet-init-" + Guid.NewGuid());
    private readonly GitletContexte _contexte;
    private readonly CommandInit _init = new();

    public CommandInitTestsUnitaires()
    {
        Directory.CreateDirectory(_dir);

        _contexte = new GitletContexte(_dir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_dir))
        {
            Directory.Delete(_dir, recursive: true);
        }
    }

    [Fact]
    public void ExpectedArgs_IsOne()
    {
        // Act & Assert
        _init.ExpectedArgs.ShouldBe(1);
    }

    [Fact]
    public void Execute_ReturnsConfirmationMessage()
    {
        // Act
        var result = _init.Execute(_contexte, ["init"]);

        // Assert
        result.ShouldBe("project initialised");
    }

    [Fact]
    public void Execute_CreatesGitletLayout()
    {
        // Act
        _init.Execute(_contexte, ["init"]);

        // Assert
        Directory.Exists(Path.Combine(_dir, ".gitlet")).ShouldBeTrue();

        Directory.Exists(Path.Combine(_dir, ".gitlet", "objects")).ShouldBeTrue();

        Directory.Exists(Path.Combine(_dir, ".gitlet", "refs", "heads")).ShouldBeTrue();

        File.ReadAllText(Path.Combine(_dir, ".gitlet", "HEAD")).ShouldBe("ref: refs/heads/main");

        File.Exists(Path.Combine(_dir, ".gitlet", "refs", "heads", "main")).ShouldBeTrue();
    }

    [Fact]
    public void Execute_CreatesInitialCommit()
    {
        // Act
        _init.Execute(_contexte, ["init"]);

        // Assert
        var commit = _contexte.State.ReadCommit();

        commit.Message.ShouldBe("initial commit");

        commit.TimeStamp.ShouldBe(DateTimeOffset.UnixEpoch);

        commit.ParentHashes.ShouldBeEmpty();

        commit.Files.ShouldBeEmpty();
    }

    [Fact]
    public void Execute_MainBranchPointsAtStoredInitialCommit()
    {
        // Act
        _init.Execute(_contexte, ["init"]);

        // Assert
        var branchHash = File.ReadAllText(_contexte.Paths.BranchFile("main"));

        _contexte.Objects.Exists("commit", branchHash).ShouldBeTrue();

        _contexte.State.CurrentBranch().ShouldBe("main");
    }

    [Fact]
    public void Execute_InitialCommitIsIdenticalInEveryRepository()
    {
        // Arrange
        _init.Execute(_contexte, ["init"]);

        var otherDir = Path.Combine(Path.GetTempPath(), "gitlet-init-" + Guid.NewGuid());

        try
        {
            Directory.CreateDirectory(otherDir);

            var other = new GitletContexte(otherDir);

            // Act
            _init.Execute(other, ["init"]);

            // Assert
            other.State.GetCommitHash().ShouldBe(_contexte.State.GetCommitHash());
        }
        finally
        {
            Directory.Delete(otherDir, recursive: true);
        }
    }

    [Fact]
    public void Execute_Twice_Throws()
    {
        // Arrange
        _init.Execute(_contexte, ["init"]);

        // Act & Assert
        Should.Throw<GitletException>(() => _init.Execute(_contexte, ["init"]))
            .Message.ShouldBe(ExceptionMessages.AlreadyInitialized);
    }
}
