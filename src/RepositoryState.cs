using System.Text.Json;
using System.Text;

namespace Gitlet;

public sealed class RepositoryState
{
    
    // private readonly string _workingDirectory;
    private readonly GitletPaths _paths;
    private readonly ObjectStore _objects;
    
    public RepositoryState(GitletPaths paths, ObjectStore objects)
    {
        _paths = paths;
        _objects = objects;
    }
    
    public Commit ReadCommit()
    {
        //HEAD中存储的是ref: refs/heads/master，是当前分支的string格式的路径
        var head = File.ReadAllText(_paths.HeadFile);
        const string prefix = "ref: refs/heads/";
        string branchCurr = head[prefix.Length..];
        
        //拼路径，拼到object下对应的文件的路径
        var currentbranche_path = _paths.BranchFile(branchCurr);
        
        //读分支文件,拿到 commit 哈希(string)？这是读取了refs/heads/分支名中的纯文本哈希字符串么？
        var commitHash = File.ReadAllText(currentbranche_path).Trim();
        
        //根据commit的hash值去读object路径下的对应的文件的内容
        var commitCurr = _objects.Read("commit",commitHash);
        
        //把字节按照utf8编码成json
        var commitCurrJson = Encoding.UTF8.GetString(commitCurr);
        //反序列化，就是把json？
        var commitCurrObj = JsonSerializer.Deserialize<Commit>(commitCurrJson);
        
        return commitCurrObj;
    }
    
    //overload in order to get the hash of the commit's father hash
    public Commit ReadCommit(string hash)
    {
        byte[] bytes = _objects.Read("commit",hash);
        string json = Encoding.UTF8.GetString(bytes);
        return JsonSerializer.Deserialize<Commit>(json)!;
    }


    public string GetCommitHash()
    {
        //HEAD中存储的是ref: refs/heads/master，是当前分支的string格式的路径
        var head = File.ReadAllText(_paths.HeadFile);
        const string prefix = "ref: refs/heads/";
        string branchCurr = head[prefix.Length..];
        
        //拼路径，拼到object下对应的文件的路径
        var currentbranche_path = _paths.BranchFile(branchCurr);
        
        //读分支文件,拿到 commit 哈希(string)？这是读取了refs/heads/分支名中的纯文本哈希字符串么？
        var commitHash = File.ReadAllText(currentbranche_path).Trim();
        
        return commitHash;
        
    }
    
    public Staging ReadStaging()
    {
        if (!File.Exists(_paths.StagingFile))
        {
            return new Staging
            {
                Additions = [],
                Removals = []
            };
        }

        string json = File.ReadAllText(_paths.StagingFile);
        
        var stagingCurr = JsonSerializer.Deserialize<Staging>(json);
        return  stagingCurr;
    }
    
    // 读 HEAD,返回当前分支名,比如 "main"
    public string CurrentBranch()
    {
        var head = File.ReadAllText(_paths.HeadFile).Trim();
        const string prefix = "ref: refs/heads/";
        return head[prefix.Length..];
    }
    
    //for merge
    //find split point 
    //这里没有考虑有两个父的情况，只考虑了list，没考虑tree
    public string FindSplitPoint(string hash1, string hash2)
    {
        //把hash1的所有祖先装进一个List<string>
        var ancestors1 = new List<string>();

        var current1 = hash1;
        
        while (true)
        {
            ancestors1.Add(current1);
            var commitCurr = ReadCommit(current1);
            if (commitCurr.ParentHashes.Count == 0) break;
            current1 = commitCurr.ParentHashes[0];
        }
        ancestors1.Reverse();//从老到小
        
        var length1 = ancestors1.Count;
        
        //把hash2的所有祖先装进一个List<string>
        var ancestors2 = new List<string>();

        var current2 = hash2;
        
        while (true)
        {
            ancestors1.Add(current2);
            var commitCurr = ReadCommit(current2);
            if (commitCurr.ParentHashes.Count == 0) break;
            current2 = commitCurr.ParentHashes[0];
        }
        ancestors2.Reverse();//从老到小
        
        var length2 = ancestors2.Count;
        
        // 从太祖开始遍历，返回第一个不同的hash的上一个
        
        var count = 0;
        
        while (count < length1 && count < length2&&ancestors1[count] == ancestors2[count])
        {
            count++;
        }
        return ancestors1[count-1];
        
    }
    // A ── B ── C ── M ── X      (current)
    //     \        /
    //     ── D ──── Y         (given)
    //BFS，广度优先算法，用来找有merge情况下的split point
    //这里处理的是邻接链表 Dictionary<string, string[]>
    //队列 
    public string FindSplitPointBfs(string hash1, string hash2)
    {
        
        // ── 第一步:从 hash1 出发,收集它自己和所有祖先 ──
        var ancestors1 = new HashSet<string>();   // 已访问的 commit,也就是 hash1 的全部祖先
        var queue1 = new Queue<string>();         // 待处理的 commit,先进先出
        queue1.Enqueue(hash1);
        
        while (queue1.Count > 0)
        {
            var current = queue1.Dequeue();       // 从队头取一个

            // Add 返回 false 表示已经在集合里了:处理过,跳过
            if (!ancestors1.Add(current))
            {
                continue;
            }

            // 把它的每一个父都放进队尾(merge commit 有两个父,两个都要放)
            foreach (var parent in ReadCommit(current).ParentHashes)
            {
                queue1.Enqueue(parent);
            }
        }
        
        // ── 第二步:从 hash2 出发,一层层往上找,第一个在 ancestors1 里的就是答案 ──
        var visited2 = new HashSet<string>();
        var queue2 = new Queue<string>();
        queue2.Enqueue(hash2);

        while (queue2.Count > 0)
        {
            var current = queue2.Dequeue();

            if (!visited2.Add(current))
            {
                continue;
            }

            // 第一个同时也是 hash1 祖先的 commit,就是最近公共祖先
            if (ancestors1.Contains(current))
            {
                return current;
            }

            foreach (var parent in ReadCommit(current).ParentHashes)
            {
                queue2.Enqueue(parent);
            }
        }

        // 正常仓库里所有 commit 都能追溯到初始 commit,不会走到这里
        throw new InvalidOperationException("No common ancestor found.");
        
    }
    
    
}