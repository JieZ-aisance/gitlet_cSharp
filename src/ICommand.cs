namespace Gitlet.Commands;

public interface ICommand
{
    // 需要几个参数(含命令名本身),用于 Incorrect operands 检查
    public int ExpectedArgs { get; }

    // 执行命令,返回要打印的内容(没有就返回空字符串)
    public string Execute(GitletContexte contexte, string[] args);
    //这里的args是数组，每个命令从数组里取自己需要的

    
}