using System.Text;
using System.Security.Cryptography;

namespace Gitlet;
//这个类用来把已经构建出来的对象放进objects目录
//为什么不是接口？
//因为接口需要至少两个实现才有意义,而 ObjectStore 只会有一个——文件系统。
public class ObjectStore
{
    //传入GitletPath
    private readonly GitletPaths _paths;
    
    //constructor
    public ObjectStore(GitletPaths paths)
    {
        _paths = paths;
    }
    
    //Write
    /*
     * Write(string type, byte[] content) 返回 string:
       
       算哈希
       用 _paths.ObjectPath(hash) 得到目标路径
       如果文件已存在,直接返回哈希(这就是去重)
       否则先建目录(Path.GetDirectoryName 取出父目录),再 File.WriteAllBytes
       返回哈希
     */
    public string Write(string type, byte[] content)
    {
        var hash = ComputeHash(type, content);//waiting for methode calcul de hash selon content
        
        var path = _paths.ObjectPath(hash);//hash to path，调用objectpath计算要写入的对象的路径
        
        if (!File.Exists(path))
        {
            //创建该path下的路径
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllBytes(path, content);
        }
        
        return hash;
    }
    
    //Read
    public byte[] Read(string hash)
    {
        return File.ReadAllBytes(_paths.ObjectPath(hash));
    }
    
    //Existe
    public bool Exists(string hash)
    {
        return File.Exists(_paths.ObjectPath(hash));
    }
    //Helpers
    private static string ComputeHash(string type, byte[] content)
    {
        //造头
        string headerText = $"{type} {content.Length}\0";
        
        //头转成字节
        byte[] header = Encoding.UTF8.GetBytes(headerText);
        
        //头+内容 拼成一个数组
        byte[] full = [.. header, .. content];
        
        //算SHA-1
        byte[] hashBytes = SHA1.HashData(full);
        
        //byte 转 string
        var hashString = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
        
        return  hashString;
    }
}