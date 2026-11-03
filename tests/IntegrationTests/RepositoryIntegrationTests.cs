using System.Text.RegularExpressions;
using Shouldly;

namespace Gitlet.Tests.IntegrationTests;

public class RepositoryIntegrationTests : IDisposable
{
    private readonly string _dir =
        Path.Combine(Path.GetTempPath(), "gitlet-cli-" + Guid.NewGuid());

    public RepositoryIntegrationTests()
    {
        Directory.CreateDirectory(_dir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_dir))
        {
            Directory.Delete(_dir, recursive: true);
        }
    }

    // helpers

    private CliResult Gitlet(params string[] args) => GitletCli.Run(_dir, args);

    private string PathOf(string name) => Path.Combine(_dir, name);

    private void WriteFile(string name, string content) => File.WriteAllText(PathOf(name), content);

    private string ReadFile(string name) => File.ReadAllText(PathOf(name));

    private void Init() => Gitlet("init");

    private void AddAndCommit(string name, string content, string message)
    {
        WriteFile(name, content);
        
        Gitlet("add", name);
        
        Gitlet("commit", message);
    }

    private static List<string> HashesIn(string log) =>
        Regex.Matches(log, @"commit\s+([0-9a-f]{40})").Select(m => m.Groups[1].Value).ToList();

    private string HeadHash() => HashesIn(Gitlet("log").Output)[0];
    

    [Fact]
    public void NoArguments_AsksForACommand()
    {
        // Act & Assert
        Gitlet().Output.ShouldBe(ExceptionMessages.PleaseEnterCommand + Environment.NewLine);
    }

    [Fact]
    public void UnknownCommand_IsRejected()
    {
        // Arrange
        Init();

        // Act & Assert
        Gitlet("frobnicate").Output.ShouldBe(ExceptionMessages.NoSuchCommand + Environment.NewLine);
    }

    [Theory]
    [InlineData("add", "a.txt")]
    [InlineData("commit", "message")]
    [InlineData("rm", "a.txt")]
    [InlineData("log")]
    [InlineData("global-log")]
    [InlineData("find", "message")]
    [InlineData("branch", "dev")]
    [InlineData("rm-branch", "dev")]
    [InlineData("status")]
    [InlineData("checkout", "dev")]
    [InlineData("reset", "abc")]
    public void CommandsBeforeInit_AreRejected(params string[] args)
    {
        // Act & Assert
        Gitlet(args).Output.ShouldBe(ExceptionMessages.NotInitialized + Environment.NewLine);
        
        Directory.Exists(PathOf(".gitlet")).ShouldBeFalse();
        
    }

    [Theory]
    [InlineData("init", "extra")]
    [InlineData("add")]
    [InlineData("add", "a.txt", "b.txt")]
    [InlineData("commit")]
    [InlineData("commit", "one", "two")]
    [InlineData("rm")]
    [InlineData("log", "extra")]
    [InlineData("global-log", "extra")]
    [InlineData("find")]
    [InlineData("branch")]
    [InlineData("rm-branch")]
    [InlineData("status", "extra")]
    [InlineData("reset")]
    [InlineData("checkout")]
    [InlineData("checkout", "a", "b")]
    [InlineData("checkout", "a", "b", "c")]
    [InlineData("checkout", "a", "b", "c", "d")]
    public void WrongNumberOfArguments_IsIncorrectOperands(params string[] args)
    {
        // Arrange
        Init();

        // Act & Assert
        Gitlet(args).Output.ShouldBe(ExceptionMessages.IncorrectOperands + Environment.NewLine);
        
    }
    

    [Fact]
    public void Init_PrintsNothing_AndCreatesRepository()
    {
        // Act
        var result = Gitlet("init");

        // Assert
        result.Output.ShouldBeEmpty();
        
        result.Error.ShouldBeEmpty();
        
        Directory.Exists(PathOf(".gitlet")).ShouldBeTrue();
        
    }

    [Fact]
    public void Errors_PrintOnlyTheMessage_NoStackTrace()
    {
        // Arrange
        Init();

        // Act
        var result = Gitlet("init");

        // Assert
        result.Output.ShouldBe(ExceptionMessages.AlreadyInitialized + Environment.NewLine);
        
        result.Error.ShouldBeEmpty();
        
    }

    
    
    [Fact]
    public void Status_OnFreshRepository_PrintsToStdout()
    {
        // Arrange
        Init();

        // Act & Assert
        Gitlet("status").Output.ShouldBe(
            "=== Branches ===\n*main\n\n" +
            "=== Staged Files ===\n\n" +
            "=== Removed Files ===\n\n" +
            "=== Modifications Not Staged For Commit ===\n\n" +
            "=== Untracked Files ===\n\n");
        
    }

    
    
    [Fact]
    public void Log_ShowsDateInTheProcessTimeZone()
    {
        // Arrange
        Init();

        // Act & Assert
        Gitlet("log").Output.ShouldContain("Thu Jan 1 00:00:00 1970 +00:00");
        
    }

    [Fact]
    public void Find_PrintsTheSameHashAsLog()
    {
        // Arrange
        Init();
        
        AddAndCommit("a.txt", "x", "target");

        // Act & Assert
        Gitlet("find", "target").Output.ShouldBe(HeadHash() + "\n");
        
    }


    [Fact]
    public void Checkout_DashDashFile_RestoresFileFromHead()
    {
        // Arrange
        Init();
        
        AddAndCommit("a.txt", "committed", "first");
        
        WriteFile("a.txt", "scribbled");

        // Act
        Gitlet("checkout", "--", "a.txt");

        // Assert
        ReadFile("a.txt").ShouldBe("committed");
    }

    [Fact]
    public void Checkout_CommitIdDashDashFile_RestoresFileFromThatCommit()
    {
        // Arrange
        Init();
        
        AddAndCommit("a.txt", "v1", "first");
        
        var firstId = HeadHash();
        
        AddAndCommit("a.txt", "v2", "second");

        // Act
        Gitlet("checkout", firstId[..8], "--", "a.txt");

        // Assert
        ReadFile("a.txt").ShouldBe("v1");
    }

    [Fact]
    public void Checkout_BranchName_SwitchesBranch()
    {
        // Arrange
        Init();
        
        Gitlet("branch", "dev");

        // Act
        Gitlet("checkout", "dev");

        // Assert
        Gitlet("status").Output.ShouldContain("*dev\n");
        
    }

    
    
    [Fact]
    public void Checkout_UnknownBranch_PrintsError()
    {
        // Arrange
        Init();

        // Act & Assert
        Gitlet("checkout", "ghost").Output.ShouldBe(ExceptionMessages.BranchNoExists + Environment.NewLine);
        
    }



    [Fact]
    public void Scenario_AddCommitLog()
    {
        // Arrange
        Init();
        
        WriteFile("hello.txt", "hello");

        // Act & Assert
        Gitlet("add", "hello.txt").Output.ShouldBeEmpty();
        
        Gitlet("status").Output.ShouldContain("=== Staged Files ===\nhello.txt\n");
        
        Gitlet("commit", "add hello").Output.ShouldBeEmpty();
        
        Gitlet("status").Output.ShouldContain("=== Staged Files ===\n\n");

        var log = Gitlet("log").Output;
        
        HashesIn(log).Count.ShouldBe(2);
        
        log.IndexOf("add hello", StringComparison.Ordinal)
            .ShouldBeLessThan(log.IndexOf("initial commit", StringComparison.Ordinal));
        
    }

    
    
    [Fact]
    public void Scenario_RemoveTrackedFileAndCommit()
    {
        // Arrange
        Init();
        
        AddAndCommit("a.txt", "x", "add a");

        // Act
        Gitlet("rm", "a.txt");

        // Assert
        File.Exists(PathOf("a.txt")).ShouldBeFalse();
        
        Gitlet("status").Output.ShouldContain("=== Removed Files ===\na.txt\n");

        // Act
        Gitlet("commit", "remove a");

        // Assert
        Gitlet("checkout", "--", "a.txt").Output
            .ShouldBe(ExceptionMessages.FileDoesNotExist + Environment.NewLine);
        
    }

    
    
    [Fact]
    public void Scenario_BranchesDivergeAndSwitchBack()
    {
        // Arrange
        Init();
        
        AddAndCommit("shared.txt", "base", "base");
        
        Gitlet("branch", "dev");
        
        Gitlet("checkout", "dev");
        
        AddAndCommit("shared.txt", "dev version", "dev edit");
        
        AddAndCommit("dev-only.txt", "d", "dev file");

        // Act
        Gitlet("checkout", "main");

        // Assert
        ReadFile("shared.txt").ShouldBe("base");
        
        File.Exists(PathOf("dev-only.txt")).ShouldBeFalse();
        
        Gitlet("log").Output.ShouldNotContain("dev edit");
        
        Gitlet("global-log").Output.ShouldContain("dev edit");

        // Act
        Gitlet("checkout", "dev");

        // Assert
        ReadFile("shared.txt").ShouldBe("dev version");
        
        ReadFile("dev-only.txt").ShouldBe("d");
        
    }

    
    
    [Fact]
    public void Scenario_ResetToEarlierCommit()
    {
        // Arrange
        Init();
        
        AddAndCommit("a.txt", "v1", "first");
        
        var firstId = HeadHash();
        
        AddAndCommit("a.txt", "v2", "second");
        
        AddAndCommit("b.txt", "b", "third");

        // Act
        Gitlet("reset", firstId);

        // Assert
        ReadFile("a.txt").ShouldBe("v1");
        
        File.Exists(PathOf("b.txt")).ShouldBeFalse();
        
        HashesIn(Gitlet("log").Output).Count.ShouldBe(2);
        
    }

    
    
    [Fact]
    public void Scenario_TwoRepositoriesAreIndependent()
    {
        // Arrange
        
        Init();
        
        AddAndCommit("a.txt", "x", "only here");
        
        var otherDir = Path.Combine(Path.GetTempPath(), "gitlet-cli-" + Guid.NewGuid());
        try
        {
            Directory.CreateDirectory(otherDir);
            
            GitletCli.Run(otherDir, "init");

            // Act & Assert
            GitletCli.Run(otherDir, "log").Output.ShouldNotContain("only here");
            
        }
        finally
        {
            Directory.Delete(otherDir, recursive: true);
        }
    }
}
