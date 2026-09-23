# gitlet en c# pour une demontration de competences en dev
### Author:Jie

## Les Classes
### 0, Blob
### 1, Commit
##### Message
##### TimeStamp
##### Parent
### 2, Branch
### 3, Head
### 4, Staging
### 5, Log
### 6, Exceptions
### 7, Repository
 
#### les actions telles que add, find, init sont dans cette classe.


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


