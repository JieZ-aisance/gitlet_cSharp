using System.Text;
using System.Security.Cryptography;

namespace Gitlet;

public class ObjectStore
{
    private readonly GitletPaths _paths;
    
    //constructor
    public ObjectStore(GitletPaths paths)
    {
        _paths = paths;
    }
    
    public string Write(string type, byte[] content)
    {
        var hash = ComputeHash(type, content);
        
        var path = _paths.ObjectPath(type, hash);
        
        if (!File.Exists(path))
        {

            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            
            File.WriteAllBytes(path, content);
        }
        
        return hash;
    }
    
    //Read
    public byte[] Read(string type,string hash)
    {
        return File.ReadAllBytes(_paths.ObjectPath(type, hash));
    }
    
    //Existe
    public bool Exists(string type, string hash)
    {
        return File.Exists(_paths.ObjectPath(type, hash));
    }
    
    
    //Helpers
    private static string ComputeHash(string type, byte[] content)
    {
        string headerText = $"{type} {content.Length}\0";
        
        byte[] header = Encoding.UTF8.GetBytes(headerText);
        
        byte[] full = [.. header, .. content];
        
        byte[] hashBytes = SHA1.HashData(full);
        
        var hashString = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
        
        return  hashString;
    }
}