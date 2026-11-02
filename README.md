# Gitlet en c# pour une demontration de competences en dev C# .NET 🇫🇷

### Author: Jie ZHAO, master en informatique de UGA(IM2AG, Grenoble), en recherche active de CDI développeuse junior, disponible immédiatement.
### Updated: 2026-11-02
### Comparable au Cahier des charges dans la production du projet Gitlet/ Specification : https://sp21.datastructur.es/materials/proj/proj2/proj2
### 🇬🇧 English version is available below the french one.

====================================================================================

## Version Française 🇫🇷
## Commandes implémentées

1. `init` — Crée un nouveau dépôt Gitlet dans le répertoire courant, avec un commit initial vide sur la branche par défaut.
2. `add <nom du fichier>` — Indexe la version actuelle d'un fichier pour le prochain commit.
3. `commit "<message>"` — Enregistre un instantané des modifications indexées sous forme d'un nouveau commit sur la branche courante.
4. `rm <nom du fichier>` — Retire un fichier de l'index et, s'il est suivi, le marque pour suppression et l'efface du répertoire de travail.
5. `log` — Affiche l'historique de la branche courante, du dernier commit jusqu'au commit initial.
6. `global-log` — Affiche tous les commits jamais créés dans le dépôt, quelle que soit la branche.
7. `find "<message>"` — Affiche les identifiants de tous les commits dont le message correspond exactement.
8. `branch <nom de branche>` — Crée une nouvelle branche pointant sur le commit courant, sans basculer dessus.
9. `rm-branch <nom de branche>` — Supprime le pointeur d'une branche ; les commits eux-mêmes sont conservés.
10. `status` — Liste les branches (la branche courante est marquée d'un `*`) ainsi que les fichiers indexés pour ajout ou suppression.
11. `checkout`
 - 11.1. `checkout -- <nom du fichier>` — Restaure un fichier dans sa version du commit courant.
 - 11.2. `checkout <id du commit> -- <nom du fichier>` — Restaure un fichier dans sa version d'un commit donné (l'identifiant peut être abrégé).
 - 11.3. `checkout <nom de branche>` — Bascule sur une autre branche et met à jour le répertoire de travail en conséquence.

## Essayer

Clonez la branche `dev` dans un dossier local vide, installez le SDK .NET 10, puis exécutez depuis le répertoire `src` :

    dotnet clean
    dotnet build
    dotnet run -- init
    dotnet run -- add <nom du fichier>
    dotnet run -- commit "<message du commit>"
    dotnet run -- rm <nom du fichier>
    dotnet run -- log
    dotnet run -- global-log
    dotnet run -- find "<message du commit>"
    dotnet run -- branch <nom de branche>
    dotnet run -- rmbranch <nom de branche>
    dotnet run -- status
    dotnet run -- checkout -- <nom du fichier>
    dotnet run -- checkout <id du commit> -- <nom du fichier>
    dotnet run -- checkout <nom de branche>



### Pour créer des fichiers de test, utilisez l'outil de votre choix (nano, vim, etc.).


====================================================================================
## English version 🇬🇧
## Implemented commands

1. `init` — Creates a new Gitlet repository in the current directory, with an initial empty commit on the default branch.
2. `add <file name>` — Stages the current version of a file for the next commit.
3. `commit "<message>"` — Saves a snapshot of the staged changes as a new commit on the current branch.
4. `rm <file name>` — Unstages a file, and if it is tracked, marks it for removal and deletes it from the working directory.
5. `log` — Shows the history of the current branch, from the latest commit back to the initial one.
6. `global-log` — Shows every commit ever made in the repository, regardless of branch.
7. `find "<message>"` — Prints the ids of all commits whose message matches exactly.
8. `branch <branch name>` — Creates a new branch pointing at the current commit, without switching to it.
9. `rm-branch <branch name>` — Deletes a branch pointer; the commits themselves are kept.
10. `status` — Lists the branches (current one marked with `*`) and the files staged for addition or removal.
11. `checkout`
- 11.1. `checkout -- <file name>` — Restores a file to its version in the current commit.
- 11.2. `checkout <commit id> -- <file name>` — Restores a file to its version in a given commit (the id can be abbreviated).
- 11.3. `checkout <branch name>` — Switches to another branch and updates the working directory to match it.


## Try it

Clone the `dev` branch into a clean local folder, install the .NET 10 SDK, then run the following from the `src` directory:

    dotnet clean
    dotnet build
    dotnet run -- init
    dotnet run -- add <file name>
    dotnet run -- commit "<commit message>"
    dotnet run -- rm <file name>
    dotnet run -- log
    dotnet run -- global-log
    dotnet run -- find "<commit message>"
    dotnet run -- branch <branch name>
    dotnet run -- rmbranch <branch name>
    dotnet run -- status
    dotnet run -- checkout -- <file name>
    dotnet run -- checkout <commit id> -- <file name>
    dotnet run -- checkout <branch name>


### Use whichever tool you prefer (nano, vim, etc.) to create test files.


====================================================================================

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

7，最终选择了课程给定的骨架，把java 换成c#



