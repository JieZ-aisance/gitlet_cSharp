using Shouldly;
using Xunit;

namespace Gitlet.Tests.UnitTests;

public class GitletPathsTests
{
    // 一个假的工作目录。GitletPaths 只拼字符串,不检查目录是否真实存在
    private static readonly string WorkDir = Path.Combine(Path.GetTempPath(), "repo");

    private readonly GitletPaths _paths = new(WorkDir);

    // 把相对路径(用 / 分隔)接到 WorkDir 后面,兼容不同系统的分隔符
    private static string Expected(string relative) =>
        Path.Combine(WorkDir, relative.Replace('/', Path.DirectorySeparatorChar));

    // ---------- 固定路径属性 ----------

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
        getPath(_paths).ShouldBe(Expected(relative), customMessage: name);
    }

    // ---------- BranchFile ----------

    [Theory]
    [InlineData("main")]
    [InlineData("dev")]
    [InlineData("feature-x")]
    public void BranchFile_IsUnderRefsHeads(string branch)
    {
        _paths.BranchFile(branch).ShouldBe(Expected($".gitlet/refs/heads/{branch}"));
    }

    // ---------- ObjectPath ----------

    [Theory]
    [InlineData("blob", "ce013625030ba8dba906f756967f9e9ca394464a",
        "objects/blobs/ce/013625030ba8dba906f756967f9e9ca394464a")]
    [InlineData("commit", "220ca7eb161fe3243a0191707dfc012b96fd05d9",
        "objects/commits/22/0ca7eb161fe3243a0191707dfc012b96fd05d9")]
    public void ObjectPath_SplitsHashIntoTwoLevelDirectory(
        string type, string hash, string relativeToGitlet)
    {
        // 前 2 位是子目录名,后 38 位是文件名;blob 和 commit 放在不同的大目录
        _paths.ObjectPath(type, hash).ShouldBe(Expected($".gitlet/{relativeToGitlet}"));
    }

    [Theory]
    [InlineData("tree")]
    [InlineData("")]
    [InlineData("Blob")] // 区分大小写
    public void ObjectPath_UnknownType_Throws(string type)
    {
        Should.Throw<ArgumentException>(() => _paths.ObjectPath(type, "abcdef"));
    }
}
