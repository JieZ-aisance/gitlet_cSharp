using System.Text.Json;
using System.Text;
using System.Globalization;

namespace Gitlet;
//TODO: any imports you need here

/** Represents a gitlet repository.
 *  TODO: It's a good idea to give a description here of what else this Class
 *  does at a high level.
 *
 *  @author TODO
 */
public class Repository
{
    private readonly string _workingDirectory;
    private readonly GitletPaths _paths;
    private readonly ObjectStore _objects;
    //把路径和被存储的数据都视为对象

    public Repository(string workingDirectory)
    {
        _workingDirectory = workingDirectory;
        _paths = new GitletPaths(workingDirectory);
        _objects = new ObjectStore(_paths);
    }
    //init
    public void Init()//Init 不需要返回任何东西——它的效果是在磁盘上建出目录和文件
    {
        //1,如果已经init过了，就报错
        if (Directory.Exists(_paths.GitletDir))// public string GitletDir => Path.Combine(_workingDirectory, ".gitlet");
        {
            throw new GitletException(ExceptionMessages.AlreadyInitialized);
        }
        
        //2,建目录
        Directory.CreateDirectory(_paths.GitletDir);
        // public string ObjectsDir => Path.Combine(GitletDir, "objects");
        Directory.CreateDirectory(_paths.ObjectsDir);
        //public string RefHeadDirec => Path.Combine(GitletDir, "refs", "heads");
        Directory.CreateDirectory(_paths.RefHeadDirec);   // 顺带建出 refs
        
        //3, 初始造commit
        //commit 的逻辑是:读当前 commit,复制它的文件映射,应用暂存区的改动,造一个新 commit,父指向旧的那个。
        var initial = new Commit
        {
            Message = "initial commit",
            TimeStamp = new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero),
            ParentHashes = [],
            Files = []//这里是字典，必须选有序字典
        };
        
        //4，序列化，得到字符串
        var json = JsonSerializer.Serialize(initial);
        
        //字符串转字节,这里是utf8编码，c#的默认编码是utf16，因此需要显示指定utf8
        byte[] bytes = Encoding.UTF8.GetBytes(json);
        
        //交给objectstore
        string commitHash = _objects.Write("commit", bytes);
        
        //5,写入 默认分支refs/heads/master
        File.WriteAllText(_paths.BranchFile("main"), commitHash);
        
        //HEAD
        File.WriteAllText(_paths.HeadFile, "ref: refs/heads/main");
        
    }
    
    //add
    //gitlet中的add一次只能add一个文件，不能add多个文件
    //具体表现为，不能一次add一个下边有多个文件的路径
    public void GitletAdd(string filePath)
    {
        // examiner si le chemin est bien ecrit

        if (!File.Exists(Path.Combine(_workingDirectory, filePath)))
        {
            throw new GitletException(ExceptionMessages.FileDoesNotExist);
        }
    

        var newHash = _objects.Write("blob", File.ReadAllBytes(Path.Combine(_workingDirectory, filePath)));
        //if we wamt to have tree like a real git in the future, maybe we can make a class of Enum( blob, commit, tree)
        //instead of sting here
        
        var content = File.ReadAllBytes(Path.Combine(_workingDirectory, filePath));
        
        //get the current commit
        var commitCurr = ReadCommit();
        
        var commitCurrFiles = commitCurr.Files; 
        
        //解析commit中的信息，看Files属性中存了哪些文件名和内容hash
        
        //read staging
        var staging = ReadStaging();
        
        staging.Removals.Remove(filePath);//避免当前文件同时出现在添加和删除里
        
        //cheak staging file
        var stagingAdditions = staging.Additions;   
        //<key, value>
        //key: file name
        //value: blob hash
        
        //if current
        if (commitCurrFiles.TryGetValue(filePath, out string? oldHash) && oldHash == newHash)
        {
            // 在 commit 里,且内容一样 → 没改过,移除
            stagingAdditions.Remove(filePath);
        }
        else
        {
            // 不在 commit 里,或者内容变了 → 加入/覆盖
            stagingAdditions[filePath] = newHash;
        }
        
        string json = JsonSerializer.Serialize(staging);
        File.WriteAllText(_paths.StagingFile, json);

    }
    
    
    //Commit
    //不改变commit对象而是新建一个
    public void GitletCommit(string message)
    {
        //检查消息非空、暂存区非空
        if (string.IsNullOrEmpty(message) )
        {
            throw new GitletException(ExceptionMessages.PleaseEnterCommitMessage);
        }

        if (ReadStaging().Additions.Count == 0 && ReadStaging().Removals.Count == 0)
        {
            throw new GitletException(ExceptionMessages.NoChangesAdded);
        }
        
        //读暂存区
        var staging = ReadStaging();
        
        //读当前commit hash
        var commitParent = ReadCommit();
        
        var commitParentHash = GetCommitHash();
        
        // var commitParentFiles = commitParent.Files; 错误写法
        var commitParentFiles = new SortedDictionary<string, string>(commitParent.Files);
        
        // 暂存的添加 → 覆盖或新增
        foreach (var pair in staging.Additions)
        {
            commitParentFiles[pair.Key] = pair.Value;
        }

        // 暂存的删除 → 移除
        foreach (string name in staging.Removals)
        {
            commitParentFiles.Remove(name);
        }
        
        //new commit
        var newCommit = new Commit
        {
            Message = message,
            TimeStamp = DateTimeOffset.Now,
            ParentHashes = [commitParentHash],
            Files = commitParentFiles
        };
        
        //
        var json = JsonSerializer.Serialize(newCommit);
        
        //字符串转字节,这里是utf8编码，c#的默认编码是utf16，因此需要显示指定utf8
        byte[] bytes = Encoding.UTF8.GetBytes(json);
        
        //交给objectstore
        string commitHash = _objects.Write("commit", bytes);
        
        //5,写入 默认分支refs/heads/master
        File.WriteAllText(_paths.BranchFile("main"), commitHash);
        
        
        //clean staging
        staging.Additions.Clear();
        staging.Removals.Clear();
        
        string stagingJson = JsonSerializer.Serialize(staging);
        File.WriteAllText(_paths.StagingFile, stagingJson);
    }
    
    //rm delete
    //如果该文件当前被暂存以待添加,取消暂存。
    //如果该文件被当前 commit 跟踪,把它暂存以待删除,
    //并且如果用户还没删,就从工作目录删除它(如果没被当前 commit 跟踪,就不要删)。
    public void GitletDelete(string filePath)
    {
        var staging = ReadStaging();
        var commitCurr = ReadCommit();
        
        var isStaged = staging.Additions.ContainsKey(filePath);
        var isCommitted = commitCurr.Files.ContainsKey(filePath);
        
        //fall
        if (!isStaged && !isCommitted)
        {
            throw new GitletException(ExceptionMessages.NoReasonToRemove);
        }
        
        //in staging
        if (isStaged)
        {
            staging.Additions.Remove(filePath);
        }
        
        //committed
        if (isCommitted)
        {
            staging.Removals.Add(filePath);

            string fullPath = Path.Combine(_workingDirectory, filePath);
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }
        }
        
        string json = JsonSerializer.Serialize(staging);
        File.WriteAllText(_paths.StagingFile, json);

    }
    
    //log
    //打印日志信息
    //first we write the log with merge information
    //格式
    // ===                                              ← 固定的分隔线
    //    commit 3e8bf1d794ca2e9ef8a4007275acf3751c7170ff  ← 这个 commit 的哈希(循环变量 hash)
    // Date: Thu Nov 9 17:01:33 2017 -0800              ← commit.TimeStamp,转本地时间再格式化
    //     Another commit message.                          ← commit.Message
    // ← 空行
    
    public string GitletLog()
    {
        //we will return in sting insted of print it here 
        //in order to facilitate the test

       var infomationToPrint = new StringBuilder();//optimisation var infomationToPrint = "";
       
       var CurrentCommit = ReadCommit();
       
       var CurrentHash = GetCommitHash();
       
       while (true)
       {
           //info to print
           
           var date = CurrentCommit.TimeStamp.ToLocalTime()
               .ToString("ddd MMM d HH:mm:ss yyyy zzz", CultureInfo.InvariantCulture); 
           
           var message = CurrentCommit.Message;
           
           //apprend
           infomationToPrint.Append("==============\n");
           infomationToPrint.Append("commit  ");
           infomationToPrint.Append(CurrentHash);
           infomationToPrint.Append("\n");
           infomationToPrint.Append("Date ");
           infomationToPrint.Append(date);
           infomationToPrint.Append("\n");
           infomationToPrint.Append(message);
           infomationToPrint.Append("\n");
           
           // CurrentCommit = ReadCommit(CurrentHash);
           //
           // CurrentHash = CurrentCommit.ParentHashes[0];
           
           if (CurrentCommit.ParentHashes.Count == 0)
           {
               break;
           }
           
           CurrentHash = CurrentCommit.ParentHashes[0];
           CurrentCommit = ReadCommit(CurrentHash);
       }
       
        return  infomationToPrint.ToString();
    }
    
    //global log
    //直接扫commits目录

    public string GitletGlobalLog()
    {
        //
        var infomationToPrint = new StringBuilder();
        foreach (var commit in Directory.GetDirectories(_paths.CommitDir))
        {
            var prefixe = Path.GetFileName(commit);

            foreach (var file in Directory.GetFiles(commit))
            {
                var hashtoprint = prefixe+Path.GetFileName(file);
                var CurrentCommit = ReadCommit(hashtoprint);
                
                var date = CurrentCommit.TimeStamp.ToLocalTime()
                    .ToString("ddd MMM d HH:mm:ss yyyy zzz", CultureInfo.InvariantCulture); 
           
                var message = CurrentCommit.Message;
                
                //apprend
                infomationToPrint.Append("==============\n");
                infomationToPrint.Append("commit  ");
                infomationToPrint.Append(hashtoprint);
                infomationToPrint.Append("\n");
                infomationToPrint.Append("Date ");
                infomationToPrint.Append(date);
                infomationToPrint.Append("\n");
                infomationToPrint.Append(message);
                infomationToPrint.Append("\n");
                
            }
        }
        return infomationToPrint.ToString();
    }
    
    //find
    //给一条提交消息,找出所有消息完全相同的 commit,打印它们的哈希。
    //如果没有就throw exceptions
    public string GitletFind(string message)
    {
        var infomationToPrint = new StringBuilder();
        var compteur = 0;
        
        foreach (var commit in Directory.GetDirectories(_paths.CommitDir))
        {
            var prefixe = Path.GetFileName(commit);
            

            foreach (var file in Directory.GetFiles(commit))
            {
                var hashtoprint = prefixe + Path.GetFileName(file);
                
                var CurrentCommit = ReadCommit(hashtoprint);

                // var date = CurrentCommit.TimeStamp.ToLocalTime()
                //     .ToString("ddd MMM d HH:mm:ss yyyy zzz", CultureInfo.InvariantCulture);

                if (message == CurrentCommit.Message)
                {
                    infomationToPrint.Append(hashtoprint);
                    
                    infomationToPrint.Append("\n");

                    compteur++;
                }
            }
        }
        
        if (compteur == 0)
        {
            throw new GitletException(ExceptionMessages.NoCommitWithMessage);
        }
        return infomationToPrint.ToString();
    }
    
    //branch
    //Creates a new branch with the given name, and points it at the current head commit. 
    //创建分支，但是不会立刻切换到新分支上
    
    public void GitletBranch(string branchName)
    {
        //already exist
        if (File.Exists(_paths.BranchFile(branchName)))
        {
            throw new GitletException(ExceptionMessages.BranchAlreadyExists);
            
        }
        //sinon
        var commitCurrHash = GetCommitHash();
        //public string RefHeadDirec => Path.Combine(GitletDir, "refs", "heads");
        File.WriteAllText(_paths.BranchFile(branchName), commitCurrHash);
        //Console.WriteLine("New branch created: " + branchName);
    }
    
    
    
    
    //Helper
    private  Commit ReadCommit()
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
    private Commit ReadCommit(string hash)
    {
        byte[] bytes = _objects.Read("commit",hash);
        string json = Encoding.UTF8.GetString(bytes);
        return JsonSerializer.Deserialize<Commit>(json)!;
    }


    private string GetCommitHash()
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
    
    private Staging ReadStaging()
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
}