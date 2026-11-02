using System.Text.Json;
using System.Text;
using System.Globalization;

namespace Gitlet;

public class Repository
{
    private readonly string _workingDirectory;
    private readonly GitletPaths _paths;
    private readonly ObjectStore _objects;

    public Repository(string workingDirectory)
    {
        _workingDirectory = workingDirectory;
        _paths = new GitletPaths(workingDirectory);
        _objects = new ObjectStore(_paths);
    }
    //init
    public void Init()
    {
        if (Directory.Exists(_paths.GitletDir))
        {
            throw new GitletException(ExceptionMessages.AlreadyInitialized);
        }
        
        Directory.CreateDirectory(_paths.GitletDir);

        Directory.CreateDirectory(_paths.ObjectsDir);

        Directory.CreateDirectory(_paths.RefHeadDirec);  

        var initial = new Commit
        {
            Message = "initial commit",
            
            TimeStamp = new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero),
            
            ParentHashes = [],
            
            Files = []
        };
        
        var json = JsonSerializer.Serialize(initial);
        
        byte[] bytes = Encoding.UTF8.GetBytes(json);
        
        string commitHash = _objects.Write("commit", bytes);
        
        File.WriteAllText(_paths.BranchFile("main"), commitHash);
        
        File.WriteAllText(_paths.HeadFile, "ref: refs/heads/main");
        
    }
    
    //add
    public void GitletAdd(string filePath)
    {
        if (!File.Exists(Path.Combine(_workingDirectory, filePath)))
        {
            throw new GitletException(ExceptionMessages.FileDoesNotExist);
        }
        
        var newHash = _objects.Write("blob", File.ReadAllBytes(Path.Combine(_workingDirectory, filePath)));
        
        var content = File.ReadAllBytes(Path.Combine(_workingDirectory, filePath));
        
        var commitCurr = ReadCommit();
        
        var commitCurrFiles = commitCurr.Files; 

        var staging = ReadStaging();
        
        staging.Removals.Remove(filePath);
        
        var stagingAdditions = staging.Additions;   
        
        if (commitCurrFiles.TryGetValue(filePath, out string oldHash) && oldHash == newHash)
        {
            stagingAdditions.Remove(filePath);
        }
        else
        {
            stagingAdditions[filePath] = newHash;
        }
        
        string json = JsonSerializer.Serialize(staging);
        
        File.WriteAllText(_paths.StagingFile, json);
    }
    
    
    //Commit
    public void GitletCommit(string message)
    {
        if (string.IsNullOrEmpty(message) )
        {
            throw new GitletException(ExceptionMessages.PleaseEnterCommitMessage);
        }

        if (ReadStaging().Additions.Count == 0 && ReadStaging().Removals.Count == 0)
        {
            throw new GitletException(ExceptionMessages.NoChangesAdded);
        }
        
        var staging = ReadStaging();

        var commitParent = ReadCommit();
        
        var commitParentHash = GetCommitHash();
        
        var commitParentFiles = new SortedDictionary<string, string>(commitParent.Files);
        
        foreach (var pair in staging.Additions)
        {
            commitParentFiles[pair.Key] = pair.Value;
        }
        
        foreach (string name in staging.Removals)
        {
            commitParentFiles.Remove(name);
        }
        
        var newCommit = new Commit
        {
            Message = message,
            TimeStamp = DateTimeOffset.Now,
            ParentHashes = [commitParentHash],
            Files = commitParentFiles
        };
        
        var json = JsonSerializer.Serialize(newCommit);

        byte[] bytes = Encoding.UTF8.GetBytes(json);
        
        string commitHash = _objects.Write("commit", bytes);
        
        var currentBranch = CurrentBranch();
        
        File.WriteAllText(_paths.BranchFile(currentBranch), commitHash);
        
        staging.Additions.Clear();
        
        staging.Removals.Clear();
        
        string stagingJson = JsonSerializer.Serialize(staging);
        
        File.WriteAllText(_paths.StagingFile, stagingJson);
    }
    
    //rm delete
    public void GitletDelete(string filePath)
    {
        var staging = ReadStaging();
        
        var commitCurr = ReadCommit();
        
        var isStaged = staging.Additions.ContainsKey(filePath);
        
        var isCommitted = commitCurr.Files.ContainsKey(filePath);
        
        if (!isStaged && !isCommitted)
        {
            throw new GitletException(ExceptionMessages.NoReasonToRemove);
        }
        
        if (isStaged)
        {
            staging.Additions.Remove(filePath);
        }

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
    public string GitletLog()
    {
       var infomationToPrint = new StringBuilder();
       
       var CurrentCommit = ReadCommit();
       
       var CurrentHash = GetCommitHash();
       
       while (true)
       {
           var date = CurrentCommit.TimeStamp.ToLocalTime()
               .ToString("ddd MMM d HH:mm:ss yyyy zzz", CultureInfo.InvariantCulture); 
           
           var message = CurrentCommit.Message;
           
           infomationToPrint.Append("==============\n");
           infomationToPrint.Append("commit  ");
           infomationToPrint.Append(CurrentHash);
           infomationToPrint.Append("\n");
           infomationToPrint.Append("Date ");
           infomationToPrint.Append(date);
           infomationToPrint.Append("\n");
           infomationToPrint.Append(message);
           infomationToPrint.Append("\n");
           
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
    public string GitletGlobalLog()
    {
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
    public void GitletBranch(string branchName)
    {
        if (File.Exists(_paths.BranchFile(branchName)))
        {
            throw new GitletException(ExceptionMessages.BranchAlreadyExists);
            
        }

        var commitCurrHash = GetCommitHash();
        
        File.WriteAllText(_paths.BranchFile(branchName), commitCurrHash);

    }
    
    //rm branch
    public void GitletRmBranch(string branchName)
    {
        if (!File.Exists(_paths.BranchFile(branchName)))
        {
            throw new GitletException(ExceptionMessages.BranchNoExists);
        }

        if (branchName == CurrentBranch())
        {
            throw new GitletException(ExceptionMessages.DeleteCurrentBranchForbiden);
        }

        File.Delete(_paths.BranchFile(branchName));
        
        Console.WriteLine("Branch deleted: " + branchName);
    }
    
    
    //status
    public string GitletStatus()
    {

        var infomationToPrint = new StringBuilder();
        
        infomationToPrint.Append("=== Branches ===\n");

        var current = CurrentBranch();
        
        var branches = Directory.GetFiles(_paths.RefHeadDirec)
            .Select(Path.GetFileName)
            .OrderBy(name => name, StringComparer.Ordinal);

        foreach (var name in branches)
        {
            if (name == current)
            {
                infomationToPrint.Append($"*{name}\n");
            }
            else
            {
                infomationToPrint.Append($"{name}\n");
            }
        }
        infomationToPrint.Append("\n");
        
        var staging = ReadStaging();

        infomationToPrint.Append("=== Staged Files ===\n");
        
        foreach (var name in staging.Additions.Keys)
        {
            
            infomationToPrint.Append($"{name}\n");
            
        }
        
        infomationToPrint.Append("\n");
        
        infomationToPrint.Append("=== Removed Files ===\n");
        
        foreach (var name in staging.Removals)
        {
            
                infomationToPrint.Append($"{name}\n");
                
        }
        
        infomationToPrint.Append("\n");
        
        //TO DO
        infomationToPrint.Append("=== Modifications Not Staged For Commit ===");
        infomationToPrint.Append("\n");
        infomationToPrint.Append("\n");
        
        //TO DO
        infomationToPrint.Append("=== Untracked Files ===");
        infomationToPrint.Append("\n");
        infomationToPrint.Append("\n");
        
        return infomationToPrint.ToString();
        
    }
    
    //checkout
    //1, checkout -- <fileName>
    public void GitletCheckoutFile(string fileName)
    {
        var commitCurr = ReadCommit();
        
        if (!commitCurr.Files.TryGetValue(fileName, out string blobHash))
        {
            throw new GitletException(ExceptionMessages.FileDoesNotExist);
        }

        byte[] content = _objects.Read("blob", blobHash);
        
        File.WriteAllBytes(Path.Combine(_workingDirectory, fileName), content);
    }
    
    //2, checkout <commit id> -- <FileName>
    public void GitletCheckoutCommitFile(string commitId, string fileName)
    {
        var commitList = new List<string>();
        
        foreach (var eachCommit in Directory.GetDirectories(_paths.CommitDir))
        {
            var prefixe = Path.GetFileName(eachCommit);
            
            foreach (var file in Directory.GetFiles(eachCommit))
            {
                var hashtoprint = prefixe + Path.GetFileName(file);
                
                commitList.Add(hashtoprint);
            }
        }

        foreach (var commitHash in commitList)
        {
            if (commitHash.StartsWith(commitId))
            {
                var commit = ReadCommit(commitHash);

                var Filelist = commit.Files;

                if (!Filelist.TryGetValue(fileName, out string blobHash))
                {
                    throw new GitletException(ExceptionMessages.FileDoesNotExist);
                }

                byte[] content = _objects.Read("blob", blobHash);
                
                File.WriteAllBytes(Path.Combine(_workingDirectory, fileName), content);
                
                return;
            }

        }
        throw new GitletException(ExceptionMessages.CommitDoesNotExist);
    }
    
    
    //3, checkout <branchName>
    public void GitletCheckoutBranch(string branchName)
    {

        if (!File.Exists(_paths.BranchFile(branchName)))
        {
            throw new GitletException(ExceptionMessages.BranchNoExists);
        }
        
        var currentBranch = CurrentBranch();
        
        if(branchName == currentBranch)
        {
            
            throw new GitletException(ExceptionMessages.ALreadyOnTheBranch);
            
        }
        

        var commitCurr = ReadCommit();

        var branchPath = _paths.BranchFile(branchName);          
        
        var targetHash = File.ReadAllText(branchPath).Trim();    
        
        var target = ReadCommit(targetHash);
        
        foreach (var fileName in target.Files.Keys)
        {
            var path = Path.Combine(_workingDirectory, fileName);
            
            if (File.Exists(path) && !commitCurr.Files.ContainsKey(fileName))
            {
                
                throw new GitletException(ExceptionMessages.UntrackedFileInTheWay);
                
            }
        }
        
        var targetFiles = target.Files;
        
        foreach (var pair in targetFiles)
        {
            var path = Path.Combine(_workingDirectory, pair.Key);
            
            var blobHash = pair.Value;
            
            byte[] content = _objects.Read("blob", blobHash);
            
            File.WriteAllBytes(path, content);
            
        }

        foreach (var fileName in commitCurr.Files.Keys)
        {
            if (!target.Files.ContainsKey(fileName))
            {
                var path = Path.Combine(_workingDirectory, fileName);
                
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }

        File.WriteAllText(_paths.HeadFile, $"ref: refs/heads/{branchName}");
        
        Console.WriteLine($"On the branch {branchName} now");
        

        var staging = ReadStaging();
        staging.Additions.Clear();
        staging.Removals.Clear();
        File.WriteAllText(_paths.StagingFile, JsonSerializer.Serialize(staging));
    
    }
    
    //reset

    public void GitletReset(string commitId)
    {
        var commitList = new List<string>();
        
        foreach (var eachCommit in Directory.GetDirectories(_paths.CommitDir))
        {
            var prefixe = Path.GetFileName(eachCommit);
            
            foreach (var file in Directory.GetFiles(eachCommit))
            {
                var hashtoprint = prefixe + Path.GetFileName(file);
                
                commitList.Add(hashtoprint);
            }
        }

        foreach (var commitHash in commitList)
        {
            if (commitHash.StartsWith(commitId)) 
            {
                var CurrentCommit = ReadCommit();
                
                var target = ReadCommit(commitHash);
                
                var files = target.Files;

                foreach (var  file in files.Keys)
                {
                    var path = Path.Combine(_workingDirectory, file);
                    
                    if (File.Exists(path) && !CurrentCommit.Files.ContainsKey(file))
                    {
                        throw new GitletException(ExceptionMessages.UntrackedFileInTheWay);
                    }
                }
                
                foreach (var pair in files)
                {
                    var path = Path.Combine(_workingDirectory, pair.Key);
                    
                    var blobHash = pair.Value;

                    byte[] content = _objects.Read("blob", blobHash);
                    
                    File.WriteAllBytes(Path.Combine(_workingDirectory, pair.Key), content);
                }
                
                foreach (var file in CurrentCommit.Files.Keys)
                {
                    if (!target.Files.ContainsKey(file))
                    {
                        var path = Path.Combine(_workingDirectory, file);
                        
                        if (File.Exists(path))
                        {
                            File.Delete(path);
                        }
                    }
                }
                
                File.WriteAllText(_paths.BranchFile(CurrentBranch()), commitHash);

                var staging = ReadStaging();
                staging.Additions.Clear();
                staging.Removals.Clear();
                File.WriteAllText(_paths.StagingFile, JsonSerializer.Serialize(staging));
                return;
            }
        }
        throw new GitletException(ExceptionMessages.CommitDoesNotExist);
    }
    
    
    //Helper
    private  Commit ReadCommit()
    {

        var head = File.ReadAllText(_paths.HeadFile);
        
        const string prefix = "ref: refs/heads/";
        
        string branchCurr = head[prefix.Length..];
        
        var currentbranche_path = _paths.BranchFile(branchCurr);
        
        var commitHash = File.ReadAllText(currentbranche_path).Trim();

        var commitCurr = _objects.Read("commit",commitHash);

        var commitCurrJson = Encoding.UTF8.GetString(commitCurr);

        var commitCurrObj = JsonSerializer.Deserialize<Commit>(commitCurrJson);
        
        return commitCurrObj;
    }
    
    
    private Commit ReadCommit(string hash)
    {
        byte[] bytes = _objects.Read("commit",hash);
        
        string json = Encoding.UTF8.GetString(bytes);
        
        return JsonSerializer.Deserialize<Commit>(json)!;
        
    }


    private string GetCommitHash()
    {

        var head = File.ReadAllText(_paths.HeadFile);
        
        const string prefix = "ref: refs/heads/";
        
        string branchCurr = head[prefix.Length..];

        var currentbranche_path = _paths.BranchFile(branchCurr);

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
    

    private string CurrentBranch()
    {
        var head = File.ReadAllText(_paths.HeadFile).Trim();
        
        const string prefix = "ref: refs/heads/";
        
        return head[prefix.Length..];
    }
    
}