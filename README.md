# Gitlet in C# — a showcase of development skills of C# .NET 🇬🇧
# Gitlet en c# pour une demontration de competences en dev C# .NET🇫🇷
### Author:Jie
### Updated:2026-10-31

## 🇬🇧Implemented commands🇬🇧 

1. init
2. add
3. commit
4. rm
5. log
6. global-log
7. find
8. branch
9. rmbranch
10. status


## Try it

Clone the `dev` branch into a clean local folder, install the .NET 10 SDK, then run the following from the `src` directory:

    dotnet clean
    dotnet build
    dotnet run -- init
    dotnet run -- add <file name>
    dotnet run -- commit <commit message>
    dotnet run -- rm <file name>
    dotnet run -- log
    dotnet run -- global-log
    dotnet run -- find <commit message>
    dotnet run -- branch <branch name>
    dotnet run -- rmbranch <branch name>
    dotnet run -- status


Use whichever tool you prefer (nano, vim, etc.) to create test files.
===========================================================================================
## 🇫🇷Commandes implémentées🇫🇷

1. init
2. add
3. commit
4. rm
5. log
6. global-log
7. find
8. branch
9. rmbranch
10. status

## Essayer

Clonez la branche `dev` dans un dossier local vide, installez le SDK .NET 10, puis exécutez depuis le répertoire `src` :

    dotnet clean
    dotnet build
    dotnet run -- init
    dotnet run -- add <nom du fichier>
    dotnet run -- commit <message du commit>
    dotnet run -- rm <nom du fichier>
    dotnet run -- log
    dotnet run -- global-log
    dotnet run -- find <message du commit>
    dotnet run -- branch <branch name>
    dotnet run -- rmbranch <branch name>
    dotnet run -- status


Pour créer des fichiers de test, utilisez l'outil de votre choix (nano, vim, etc.).
===========================================================================================
## 🇨🇳已经开发完的功能：🇨🇳
1.init
2.add
3.commit
4.log
5.global log
6.find

如果你想想尝试，可以吧dev分支上的代码克隆到你本地的干净文件夹里，安装好环境后执行
到src目录下执行：

1.dotnet clean
2.dotnet build
3.dotnet run -- init
4.dotnet run -- add <str path of file>
5.dotnet run -- commit <str message of commit>
6.dotnet run -- log
7.dotnet run -- global log
8.dotnet run -- find<str message of commit>

其他创建文件的命令行工具如nano, vim 等请视自己电脑的环境选用

===========================================================================================

这个项目来自我阅读https://sp21.datastructur.es/materials/proj/proj2/proj2#the-commands， <br>
并观看视频后，在视频给出的骨架基础上逐步完善的。 <br>
做这个项目的目的是找到一份后端开发的junior工作，base法国。 <br>
设计思路思考过程用中文记录， 考虑到目标读者，项目完成到一定节点后会把设计思路翻译成法语和英文<br>
<br>
 side tips
1, git版本控制之所以能实现没改动的文件不重复占空间，是因为使用blob内容寻址。
blob只根据文件内容进行哈希，不存文件名等信息（此处区别哈希和加密的概念，哈希不可逆，加密可逆）
因此相同内容的blob即使add到暂存区多次，即使被commit多次引用，objects目录里始终只有一个blob对象。
2，gitlet只有两个对象 blob和commit
commit ──→ blob（git中多一个tree）
3，Gitlet 只管一层平铺文件,不能管理嵌套文件，因为没有tree。
4,merge功能先不写，但是知道常规操作。如果需要吧dev分支merge到main，需要先switch到main，然后git merge dev
5，merge后被merge的分支不会主动删除，需要手动删除

6,真实git的结构，给我的项目architecture做参考

![截屏2026-09-23 12.33.55.png](../../../var/folders/zb/9xsb0_8d76lb4m66ntd1nzk80000gn/T/TemporaryItems/NSIRD_screencaptureui_dFkkEq/%E6%88%AA%E5%B1%8F2026-09-23%2012.33.55.png)

7，最终选择了课程给定的骨架，把java 换成c#



