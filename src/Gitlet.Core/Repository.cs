namespace Gitlet.Core;

public class Repository
{
    //init
    public void init()
    {
        //获取当前工作目录
        string cwdirectory = Directory.GetCurrentDirectory();
        Commit initial = new Commit(message: "initial commit", parent: null);
        //branch?我们需要创建一个master
    }
    
    //commit
    public void commit()
    {
        // /*1，消息为空或全空白 → Please enter a commit message.
        //   2，暂存区的添加和删除都为空 → No changes added to the commit.
        //   3，读取当前 HEAD 指向的提交,把它的文件映射复制一份作为起点
        //   4，把暂存区里待添加的条目覆盖或加入这份映射
        //   5，把暂存区里待删除的文件名从这份映射中移除
        //   6，时间戳取当前时刻
        //   7，父哈希 = 当前 HEAD 指向的提交哈希(单元素列表)
        //   8，序列化新提交,写入 objects,得到它的哈希
        //   9，把当前分支的引用指向这个新哈希
        //   10，清空暂存区
        //  */
    }
    
    //branch

}