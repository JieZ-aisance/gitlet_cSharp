using Shouldly;

namespace Gitlet.Tests.UnitTests;

public class GitletPathsTests
{
    
    private static readonly string WorkDir = Path.Combine(Path.GetTempPath(), "repo");

    private readonly GitletPaths _paths = new GitletPaths(WorkDir);

    //especially for windows env
    private static string Expected(string relative) =>
        Path.Combine(WorkDir, relative.Replace('/', Path.DirectorySeparatorChar));

    
    
    public static TheoryData<string, Func<GitletPaths, string>, string> FixedPaths => new()
    {
        { "GitletDir",    p => p.GitletDir,    ".gitlet" },
        { "ObjectsDir",   p => p.ObjectsDir,   ".gitlet/objects" },
        { "BlobDir",      p => p.BlobDir,      ".gitlet/objects/blobs" },
        { "CommitDir",    p => p.CommitDir,    ".gitlet/objects/commits" },
        { "RefHeadDirec", p => p.RefHeadDirec, ".gitlet/refs/heads" },
        { "HeadFile",     p => p.HeadFile,     ".gitlet/HEAD" },
        { "StagingFile",  p => p.StagingFile,  ".gitlet/staging" },
    };

    [Theory]
    [MemberData(nameof(FixedPaths))]
    public void FixedPath_IsBuiltUnderWorkingDirectory(
        string name, Func<GitletPaths, string> getPath, string relative)
    {
        // Arrange
        var expected = Expected(relative);
        
        // Act
        var actual = getPath(_paths);

        // Assert
        actual.ShouldBe(expected, customMessage: name);
    }


    [Theory]
    [InlineData("main")]
    [InlineData("dev")]
    [InlineData("feature-x")]
    public void BranchFile_IsUnderRefsHeads(string branch)
    {
        // Arrange
        var expected = Expected(".gitlet/refs/heads/" + branch);

        // Act
        var actual = _paths.BranchFile(branch);

        // Assert
        actual.ShouldBe(expected);
    }


    [Theory]
    [InlineData("blob", "ce013625030ba8dba906f756967f9e9ca394464a",
        ".gitlet/objects/blobs/ce/013625030ba8dba906f756967f9e9ca394464a")]
    [InlineData("commit", "220ca7eb161fe3243a0191707dfc012b96fd05d9",
        ".gitlet/objects/commits/22/0ca7eb161fe3243a0191707dfc012b96fd05d9")]
    public void ObjectPath_SplitsHashIntoTwoLevels(string type, string hash, string relative)
    {
        // Arrange
        var expected = Expected(relative);

        // Act
        var actual = _paths.ObjectPath(type, hash);

        // Assert
        actual.ShouldBe(expected);
    }
    

    [Theory]
    [InlineData("tree")]
    [InlineData("")]
    [InlineData("Blob")] //区分大小写
    public void ObjectPath_UnknownType_Throws(string type)
    {
        
        Should.Throw<ArgumentException>(() => _paths.ObjectPath(type, "abcdef"));
    }
}
