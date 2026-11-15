using System.Globalization;
using System.Text.RegularExpressions;
using Gitlet.Commands;
using Shouldly;

namespace Gitlet.Tests.UnitTests;

public class CommandLogTestsUnitaires : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "gitlet-log-" + Guid.NewGuid());
    private readonly GitletContexte _contexte;
    private readonly CommandLog _log = new();

    public CommandLogTestsUnitaires()
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
        _log.ExpectedArgs.ShouldBe(1);
    }

    [Fact]
    public void Execute_FreshRepository_HasExactLayout()
    {
        // Arrange
        var hash = _contexte.Repository.GetCommitHash();

        var date = DateTimeOffset.UnixEpoch.ToLocalTime()
            .ToString("ddd MMM d HH:mm:ss yyyy zzz", CultureInfo.InvariantCulture);

        // Act
        var result = _log.Execute(_contexte, ["log"]);

        // Assert
        result.ShouldBe($"==============\ncommit  {hash}\nDate {date}\ninitial commit\n");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(5)]
    public void Execute_HasOneEntryPerCommit_NewestFirst(int count)
    {
        // Arrange
        var expectedHashes = new List<string> { _contexte.Repository.GetCommitHash() };

        for (var i = 1; i <= count; i++)
        {
            File.WriteAllText(Path.Combine(_dir, "a.txt"), $"v{i}");

            new CommandAdd().Execute(_contexte, ["add", "a.txt"]);

            new CommandCommit().Execute(_contexte, ["commit", $"message {i}"]);

            // 新的 commit 排在最前面
            expectedHashes.Insert(0, _contexte.Repository.GetCommitHash());
        }

        // Act
        var result = _log.Execute(_contexte, ["log"]);

        // Assert
        var hashes = Regex.Matches(result, @"commit\s+([0-9a-f]{40})").Select(m => m.Groups[1].Value).ToList();

        hashes.ShouldBe(expectedHashes);

        result.ShouldContain($"message {count}\n");

        result.ShouldEndWith("initial commit\n");
    }

    [Fact]
    public void Execute_DoesNotIncludeCommitsFromOtherBranches()
    {
        // Arrange
        new CommandBranch().Execute(_contexte, ["branch", "dev"]);

        new CommandCheckoutBranch().Execute(_contexte, ["checkout", "dev"]);

        File.WriteAllText(Path.Combine(_dir, "a.txt"), "x");

        new CommandAdd().Execute(_contexte, ["add", "a.txt"]);

        new CommandCommit().Execute(_contexte, ["commit", "only on dev"]);

        new CommandCheckoutBranch().Execute(_contexte, ["checkout", "main"]);

        // Act
        var result = _log.Execute(_contexte, ["log"]);

        // Assert
        result.ShouldNotContain("only on dev");

        result.ShouldContain("initial commit");
    }

    [Fact]
    public void Execute_AfterBranchSwitch_ShowsHistoryOfThatBranch()
    {
        // Arrange
        new CommandBranch().Execute(_contexte, ["branch", "dev"]);

        new CommandCheckoutBranch().Execute(_contexte, ["checkout", "dev"]);

        File.WriteAllText(Path.Combine(_dir, "a.txt"), "x");

        new CommandAdd().Execute(_contexte, ["add", "a.txt"]);

        new CommandCommit().Execute(_contexte, ["commit", "only on dev"]);

        // Act
        var result = _log.Execute(_contexte, ["log"]);

        // Assert
        result.ShouldContain("only on dev\n");

        result.ShouldEndWith("initial commit\n");
    }
}
