using System.Text;
using Shouldly;

namespace Gitlet.Tests.UnitTests;

public class ObjectStoreTests : IDisposable
{
    // 每个测试一个独立的临时目录,测试结束后删除
    private readonly string _workDir =
        Path.Combine(Path.GetTempPath(), "gitlet-test-" + Guid.NewGuid());

    private readonly GitletPaths _paths;
    private readonly ObjectStore _store;

    public ObjectStoreTests()
    {
        _paths = new GitletPaths(_workDir);
        _store = new ObjectStore(_paths);
    }

    public void Dispose()
    {
        if (Directory.Exists(_workDir))
        {
            Directory.Delete(_workDir, recursive: true);
        }
    }

    private static byte[] Bytes(string text) => Encoding.UTF8.GetBytes(text);

    private const string MissingHash = "0123456789abcdef0123456789abcdef01234567";

    // 多组内容:文本、空、二进制(含 0x00 和非法 UTF-8)
    public static TheoryData<byte[]> SampleContents => new()
    {
        Bytes("hello gitlet"),
        Bytes("你好"),
        Array.Empty<byte>(),
        new byte[] { 0x00, 0xFF, 0xFE, 0x80, 0x0A, 0x00 },
    };

    // ---------- Write:哈希 ----------

    [Theory]
    // 预期值全部来自 `printf '<type> <len>\0<content>' | shasum`,与真实 git 一致
    [InlineData("blob", "hello", "b6fc4c620b67d95f953a5c1c1230aaab5db5a1b0")]
    [InlineData("blob", "", "e69de29bb2d1d6434b8b29ae775ad8c2e48c5391")]
    [InlineData("blob", "a", "2e65efe2a145dda7ee51d1741299f848e5bf752e")]
    [InlineData("blob", "test content\n", "d670460b4b4aece5915caf5c68d12f560a9fe3e4")]
    [InlineData("blob", "你好", "f3ca6104089db6e23891a35bfa058d287281c476")]
    // 类型是哈希头的一部分:同样的 "hello",类型不同,哈希不同
    [InlineData("commit", "hello", "34f5fae8d15abafca1ab4a596faab46b4583d8db")]
    public void Write_ReturnsGitCompatibleHash(string type, string content, string expectedHash)
    {
        _store.Write(type, Bytes(content)).ShouldBe(expectedHash);
    }

    [Theory]
    [MemberData(nameof(SampleContents))]
    public void Write_HashIs40LowercaseHexChars(byte[] content)
    {
        var hash = _store.Write("blob", content);

        hash.Length.ShouldBe(40);
        hash.ShouldAllBe(c => Uri.IsHexDigit(c) && !char.IsUpper(c));
    }

    [Theory]
    [InlineData("a", "b")]
    [InlineData("hello", "hello ")]
    [InlineData("", "\0")]
    public void Write_DifferentContent_GivesDifferentHashes(string first, string second)
    {
        _store.Write("blob", Bytes(first)).ShouldNotBe(_store.Write("blob", Bytes(second)));
    }

    // ---------- Write:落盘 ----------

    [Theory]
    [InlineData("blob")]
    [InlineData("commit")]
    public void Write_CreatesFileInDirectoryOfItsType(string type)
    {
        var hash = _store.Write(type, Bytes("hello"));
        var path = _paths.ObjectPath(type, hash);

        File.Exists(path).ShouldBeTrue();
        path.ShouldStartWith(type == "blob" ? _paths.BlobDir : _paths.CommitDir);
    }

    [Theory]
    [MemberData(nameof(SampleContents))]
    public void Write_StoresRawContentWithoutHeader(byte[] content)
    {
        var hash = _store.Write("blob", content);

        File.ReadAllBytes(_paths.ObjectPath("blob", hash)).ShouldBe(content);
    }

    [Fact]
    public void Write_SameContentTwice_ReturnsSameHashAndKeepsOneFile()
    {
        var first = _store.Write("blob", Bytes("hello"));
        var second = _store.Write("blob", Bytes("hello"));

        second.ShouldBe(first);
        Directory.GetFiles(_paths.BlobDir, "*", SearchOption.AllDirectories)
            .Length.ShouldBe(1);
    }

    [Theory]
    [InlineData("tree")]
    [InlineData("")]
    [InlineData("BLOB")]
    public void Write_UnknownType_Throws(string type)
    {
        Should.Throw<ArgumentException>(() => _store.Write(type, Bytes("x")));
    }

    // ---------- Read ----------

    [Theory]
    [MemberData(nameof(SampleContents))]
    public void Read_ReturnsWhatWasWritten(byte[] content)
    {
        var hash = _store.Write("blob", content);

        _store.Read("blob", hash).ShouldBe(content);
    }

    [Theory]
    [InlineData("blob")]
    [InlineData("commit")]
    public void Read_MissingObject_Throws(string type)
    {
        // FileNotFoundException / DirectoryNotFoundException 都是 IOException 的子类
        Should.Throw<IOException>(() => _store.Read(type, MissingHash));
    }

    // ---------- Exists ----------

    [Theory]
    [InlineData("blob", "blob", true)]
    [InlineData("commit", "commit", true)]
    [InlineData("blob", "commit", false)] // 同一个哈希,换一种类型就找不到
    [InlineData("commit", "blob", false)]
    public void Exists_DependsOnTypeItWasWrittenWith(
        string writtenType, string queriedType, bool expected)
    {
        var hash = _store.Write(writtenType, Bytes("hello"));

        _store.Exists(queriedType, hash).ShouldBe(expected);
    }

    [Theory]
    [InlineData("blob")]
    [InlineData("commit")]
    public void Exists_NeverWritten_ReturnsFalse(string type)
    {
        _store.Exists(type, MissingHash).ShouldBeFalse();
    }
}
