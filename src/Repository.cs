using System.Text.Json;
using System.Text;

namespace Gitlet;

public sealed class Repository
{
    
    private readonly string _workingDirectory;
    private readonly GitletPaths _paths;
    private readonly ObjectStore _objects;
    
    public Repository(GitletPaths paths, ObjectStore objects, string workingDirectory)
    {
        _paths = paths;
        _objects = objects;
        _workingDirectory = workingDirectory;
    }
    
    public Commit ReadCommit()
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

    
    public Commit ReadCommit(string hash)
    {
        byte[] bytes = _objects.Read("commit",hash);
        
        string json = Encoding.UTF8.GetString(bytes);
        
        return JsonSerializer.Deserialize<Commit>(json)!;
        
    }


    public string GetCommitHash()
    {
  
        var head = File.ReadAllText(_paths.HeadFile);
        
        const string prefix = "ref: refs/heads/";
        
        string branchCurr = head[prefix.Length..];
        
        var currentbranche_path = _paths.BranchFile(branchCurr);

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
    

    public string CurrentBranch()
    {
        
        var head = File.ReadAllText(_paths.HeadFile).Trim();
        
        const string prefix = "ref: refs/heads/";
        
        return head[prefix.Length..];
        
    }
    
   
    public string FindSplitPoint(string hash1, string hash2)
    {
      
        var ancestors1 = new List<string>();

        var current1 = hash1;
        
        while (true)
        {
            ancestors1.Add(current1);
            
            var commitCurr = ReadCommit(current1);
            
            if (commitCurr.ParentHashes.Count == 0) break;
            
            current1 = commitCurr.ParentHashes[0];
            
        }
        
        ancestors1.Reverse();
        
        var length1 = ancestors1.Count;
        
        var ancestors2 = new List<string>();

        var current2 = hash2;
        
        while (true)
        {
            ancestors2.Add(current2);
            
            var commitCurr = ReadCommit(current2);
            
            if (commitCurr.ParentHashes.Count == 0) break;
            
            current2 = commitCurr.ParentHashes[0];
            
        }
        
        ancestors2.Reverse();
        
        var length2 = ancestors2.Count;
        
        var count = 0;
        
        while (count < length1 && count < length2&&ancestors1[count] == ancestors2[count])
        {
            count++;
        }
        return ancestors1[count-1];
        
    }
 
    
    public string FindSplitPointBfs(string hash1, string hash2)
    {
        
        var ancestors1 = new HashSet<string>();   
        
        var queue1 = new Queue<string>();         
        
        queue1.Enqueue(hash1);
        
        while (queue1.Count > 0)
        {
            var current = queue1.Dequeue();      
            
            if (!ancestors1.Add(current))
            {
                continue;
            }


            foreach (var parent in ReadCommit(current).ParentHashes)
            {
                queue1.Enqueue(parent);
            }
        }
        

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


            if (ancestors1.Contains(current))
            {
                return current;
            }

            foreach (var parent in ReadCommit(current).ParentHashes)
            {
                queue2.Enqueue(parent);
            }
        }


        throw new InvalidOperationException("No common ancestor found.");
        
    }
    


    public void WriteConflict(string name, string? currHash, string? givenHash,
        SortedDictionary<string, string> files)
    {
        var currText = currHash == null ? "" : Encoding.UTF8.GetString(_objects.Read("blob", currHash));
        
        var givenText = givenHash == null ? "" : Encoding.UTF8.GetString(_objects.Read("blob", givenHash));

        var conflict = "<<<<<<< HEAD\n" + currText + "=======\n" + givenText + ">>>>>>>\n";
        
        var bytes = Encoding.UTF8.GetBytes(conflict);

        File.WriteAllBytes(Path.Combine(_workingDirectory, name), bytes);
        
        files[name] = _objects.Write("blob", bytes);
        
    }
    
}