namespace Gitlet;

public sealed class Staging
{
    //属性
    //有两种东西，对应两种带提交的改动
    public SortedDictionary<string, string> Additions { get; init; }
    //待添加的文件:key 是文件名,value 是该文件内容对应的 blob 哈希
    
    public SortedSet<string> Removals { get; init; }
    //为什么是 set:待删除的只需要文件名,不需要内容。而且同一个文件 rm 两次也只该记一次——set 天然不重复。
    
    //set是一组不重复则值，只关心某个东西在不在里面，不关系他出现了几次
    //set是只有key没有value的字典
    //SortedSet<T>	红黑树	O(log n)	按值排序
    //HashSet<T>	哈希表	O(1)	不确定
    
 
    
}
    
