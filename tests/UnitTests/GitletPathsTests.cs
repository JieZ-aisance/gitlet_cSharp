using Shouldly;

namespace Gitlet.Tests.UnitTests;

public class GitletPathsTests
{
    //一个假的工作目录。GitletPaths 只拼字符串,不检查目录是否真实存在
    private static readonly string WorkDir = Path.Combine(Path.GetTempPath(), "repo");

    private readonly GitletPaths _paths = new GitletPaths(WorkDir);

    //把相对路径(用 / 分隔)接到 WorkDir 后面,兼容不同系统的分隔符
    //Windows 的分隔符是 \,不转换的话测试在 Windows 上会失败
    private static string Expected(string relative) =>
        Path.Combine(WorkDir, relative.Replace('/', Path.DirectorySeparatorChar));

    // ---------- 固定路径属性 ----------

    //每一行三列:属性名(失败时提示用)、怎么读这个属性、期望的相对路径
    //p => p.GitletDir 的意思是:给一个 GitletPaths 对象 p,返回它的 GitletDir
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
        var expected = Expected(relative);

        //把 _paths 交给这一行的 lambda,读出对应的属性
        var actual = getPath(_paths);

        actual.ShouldBe(expected, customMessage: name);
    }

    // ---------- BranchFile:给分支名,返回 refs/heads/ 下的文件 ----------

    [Theory]
    [InlineData("main")]
    [InlineData("dev")]
    [InlineData("feature-x")]
    public void BranchFile_IsUnderRefsHeads(string branch)
    {
        var expected = Expected(".gitlet/refs/heads/" + branch);

        var actual = _paths.BranchFile(branch);

        actual.ShouldBe(expected);
    }

    // ---------- ObjectPath:哈希前 2 位做子目录,后 38 位做文件名 ----------

    //blob 和 commit 放在不同的大目录
    [Theory]
    [InlineData("blob", "ce013625030ba8dba906f756967f9e9ca394464a",
        ".gitlet/objects/blobs/ce/013625030ba8dba906f756967f9e9ca394464a")]
    [InlineData("commit", "220ca7eb161fe3243a0191707dfc012b96fd05d9",
        ".gitlet/objects/commits/22/0ca7eb161fe3243a0191707dfc012b96fd05d9")]
    public void ObjectPath_SplitsHashIntoTwoLevels(string type, string hash, string relative)
    {
        var expected = Expected(relative);

        var actual = _paths.ObjectPath(type, hash);

        actual.ShouldBe(expected);
    }

    // ---------- ObjectPath:不认识的类型要报错 ----------

    [Theory]
    [InlineData("tree")]
    [InlineData("")]
    [InlineData("Blob")] //区分大小写
    public void ObjectPath_UnknownType_Throws(string type)
    {
        Should.Throw<ArgumentException>(() => _paths.ObjectPath(type, "abcdef"));
    }
}
