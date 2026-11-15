using System.Globalization;
using System.Text.RegularExpressions;
using Gitlet.Commands;
using Shouldly;

namespace Gitlet.Tests.UnitTests;

public class CommandGlobalLogTestsUnitaires : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "gitlet-globallog-" + Guid.NewGuid());
    private readonly GitletContexte _contexte;
    private readonly CommandGlobalLog _globalLog = new();

    public CommandGlobalLogTestsUnitaires()
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
    public void ExpectedArgs_IsOne()
    {
        // Act & Assert
        _globalLog.ExpectedArgs.ShouldBe(1);
    }

    [Fact]
    public void Execute_FreshRepository_HasExactLayout()
    {
        // Arrange
        var hash = _contexte.State.GetCommitHash();

        var date = DateTimeOffset.UnixEpoch.ToLocalTime()
            .ToString("ddd MMM d HH:mm:ss yyyy zzz", CultureInfo.InvariantCulture);

        // Act
        var result = _globalLog.Execute(_contexte, ["global-log"]);

        // Assert
        result.ShouldBe($"==============\ncommit  {hash}\nDate {date}\ninitial commit\n");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(3)]
    public void Execute_HasOneEntryPerCommit(int extraCommits)
    {
        // Arrange
        var expectedHashes = new List<string> { _contexte.State.GetCommitHash() };

        for (var i = 0; i < extraCommits; i++)
        {
            File.WriteAllText(Path.Combine(_dir, "a.txt"), $"v{i}");

            new CommandAdd().Execute(_contexte, ["add", "a.txt"]);

            new CommandCommit().Execute(_contexte, ["commit", $"c{i}"]);

            expectedHashes.Add(_contexte.State.GetCommitHash());
        }

        // Act
        var result = _globalLog.Execute(_contexte, ["global-log"]);

        // Assert
        var hashes = Regex.Matches(result, @"commit\s+([0-9a-f]{40})").Select(m => m.Groups[1].Value).ToList();

        // global-log 不保证顺序,只比较集合
        hashes.ShouldBe(expectedHashes, ignoreOrder: true);
    }

    [Fact]
    public void Execute_IncludesCommitsFromOtherBranches()
    {
        // Arrange
        new CommandBranch().Execute(_contexte, ["branch", "dev"]);

        new CommandCheckoutBranch().Execute(_contexte, ["checkout", "dev"]);

        File.WriteAllText(Path.Combine(_dir, "a.txt"), "x");

        new CommandAdd().Execute(_contexte, ["add", "a.txt"]);

        new CommandCommit().Execute(_contexte, ["commit", "only on dev"]);

        new CommandCheckoutBranch().Execute(_contexte, ["checkout", "main"]);

        // Act
        var result = _globalLog.Execute(_contexte, ["global-log"]);

        // Assert
        result.ShouldContain("only on dev\n");

        result.ShouldContain("initial commit\n");
    }

    [Fact]
    public void Execute_IncludesCommitsNoLongerReachableFromAnyBranch()
    {
        // Arrange
        var initialHash = _contexte.State.GetCommitHash();

        File.WriteAllText(Path.Combine(_dir, "a.txt"), "x");

        new CommandAdd().Execute(_contexte, ["add", "a.txt"]);

        new CommandCommit().Execute(_contexte, ["commit", "orphan"]);

        // reset 回初始 commit,"orphan" 不再属于任何分支
        new CommandReset().Execute(_contexte, ["reset", initialHash]);

        // Act
        var result = _globalLog.Execute(_contexte, ["global-log"]);

        // Assert
        result.ShouldContain("orphan\n");
    }
}
