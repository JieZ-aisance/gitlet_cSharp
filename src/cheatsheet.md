// ═══════════════ 路径拼接(System.IO.Path,纯字符串运算,不碰磁盘) ═══════════════

Path.Combine(a, b, c)
// 拼路径,自动用当前系统的分隔符(macOS 是 /,Windows 是 \)
// 永远用它,不要手写斜杠 —— 你的 CI 有 windows-latest,手写的会当场炸
// 对应 Java: Utils.join

Path.GetDirectoryName(path)
// 去掉最后一段,拿到父目录
// ".gitlet/objects/ce/0136..." → ".gitlet/objects/ce"
// 返回 string?(可能 null),所以常写成 GetDirectoryName(path)!

Path.GetFileName(path)
// 只要最后一段
// ".gitlet/objects/ce/0136..." → "0136..."

Path.GetTempPath()
// 系统临时目录,写测试时用来建隔离的仓库


// ═══════════════ 目录操作(System.IO.Directory) ═══════════════

Directory.Exists(path)          // 目录在不在
Directory.CreateDirectory(path) // 建目录。会递归建出中间层;已存在时静默返回,不报错
Directory.GetFiles(path)        // 列出该目录下的文件(完整路径),不含子目录
// ⚠ 顺序不确定!status 要求字典序,必须自己排
// 对应 Java: Utils.plainFilenamesIn
Directory.Delete(path, true)    // 递归删除,测试清理用


// ═══════════════ 文件操作(System.IO.File) ═══════════════

File.Exists(path)                  // 文件在不在
File.ReadAllBytes(path)            // 读成 byte[] —— blob 必须用这个
File.WriteAllBytes(path, bytes)    // 写字节。⚠ 父目录必须已存在,它不会帮你建
File.ReadAllText(path)             // 读成 string —— HEAD、refs、staging 用这个
File.WriteAllText(path, text)      // 写文本
File.Delete(path)                  // 删文件。rm 命令要用
// 对应 Java: Utils.readContents / writeContents / restrictedDelete

// ⚠ blob 绝不能用 ReadAllText:
//   二进制文件会被 UTF-8 解码破坏,换行符可能被转换,哈希就不一致了


// ═══════════════ 哈希(System.Security.Cryptography + System.Text) ═══════════════

SHA1.HashData(bytes)               // 算 SHA-1,返回 20 字节
Convert.ToHexStringLower(bytes)    // 字节转 40 字符小写十六进制
// ⚠ 必须小写。Convert.ToHexString 是大写
Encoding.UTF8.GetBytes(str)        // 字符串转字节。显式写 UTF8,别依赖系统默认
Encoding.UTF8.GetString(bytes)     // 字节转字符串
// 对应 Java: Utils.sha1


// ═══════════════ 序列化(System.Text.Json) ═══════════════

JsonSerializer.Serialize(obj)           // 对象 → JSON 字符串
JsonSerializer.Deserialize<T>(json)      // JSON → 对象,需要无参构造函数
// 对应 Java: Utils.readObject / writeObject / serialize
// 区别:Java 产出二进制(要 DumpObj 才能看),我们产出 JSON,cat 直接可读


// ═══════════════ 字符串 ═══════════════

hash[..2]                          // 前 2 个字符
hash[2..]                          // 第 3 个字符到末尾
str.StartsWith(prefix)             // checkout 的缩写 id 查找要用
str.Substring(start, length)
string.IsNullOrWhiteSpace(str)     // commit 检查消息是否为空
str.Trim()                         // 读 HEAD 时去掉可能的尾随换行


// ═══════════════ 集合 ═══════════════

SortedDictionary<string, string>   // 有序字典。Commit.Files 和 Staging.Additions 用
// ⚠ 必须有序:普通 Dictionary 序列化时键序不定
//   → 同样内容算出不同哈希 → 内容寻址失效
SortedSet<string>                  // 有序集合。Staging.Removals 用
Queue<T>                           // 队列。merge 找 split point 时 BFS 要用
HashSet<T>                         // 哈希集合。BFS 记录访问过的节点
List<string>                       // Commit.ParentHashes 用


// ═══════════════ 排序(System.Linq) ═══════════════

items.OrderBy(x => x, StringComparer.Ordinal)
// 字典序排序。⚠ 必须用 Ordinal,不能用默认的文化相关比较
// 否则不同 locale 下结果不同。status 要求字典序输出