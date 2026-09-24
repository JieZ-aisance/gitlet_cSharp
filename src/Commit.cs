namespace Gitlet;


/** Represents a gitlet commit object.
 *  TODO: It's a good idea to give a description here of what else this Class
 *  does at a high level.
 *
 *  @author TODO
 */
public class Commit
{
    //字段 field 存数据的变量
    //private readonly DateTime _timeStamp;
    //属性 proprety 一对方法伪装成变量。
    //public string Author{get;set};
    //属性可以拆成字段+方法

    //服务类和数据类
    //服务类类似ObjectStore通常拆成字段，必须有构造函数，否则没法传数据
    //数据类类似commit通常存成属性。没有构造函数

    /** The message of this Commit. */
    //属性
    public string Message { get; set; }
    //public string Parents { get; set; }
    public List<string> ParentHashes { get; init; }//因为有两个父
    public DateTimeOffset TimeStamp { get; init; }
    //File映射？？？这里是重点我还没完全想明白
    //commit 必须记录这次提交包含哪些文件、各自的 blob 哈希是什么。没有它,checkout 无法恢复文件。
    public SortedDictionary<string, string> Files { get; init; }
    
 
}