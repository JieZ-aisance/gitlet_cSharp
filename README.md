# Gitlet en C#  🇫🇷

### Author: Jie ZHAO, diplomée M2 en informatique 2026, en recherche active d'un CDI.
### Updated: 2026-11-02
### Cahier des charges du projet Gitlet: https://sp21.datastructur.es/materials/proj/proj2/proj2
### 🇬🇧 English version is available below the french one.
### Pour le détail du raisonnement et des algorithmes, consultez le code de la branche `dev` ; sur la branche `main`, les commentaires ont été retirés pour garder un code propre.

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
12. `reset <id du commit>` — Ramène la branche courante et le répertoire de travail à l'état d'un commit donné, et vide l'index (l'identifiant peut être abrégé).

## Démarrage rapide

### 1. Récupérer et compiler le projet

    cd ~
    git clone https://github.com/JieZ-aisance/gitlet_cSharp.git
    cd gitlet_cSharp
    dotnet build src

### 2. Vérifier que tout fonctionne

    ./scripts/smoke_test.sh

Ce script exécute les principales commandes dans un dossier temporaire, compare leur sortie avec le résultat attendu, puis supprime tout ce qu'il a créé. Il doit se terminer par `Failed: 0`.

### 3. Essayer Gitlet

Travaillez dans un dossier séparé, afin que le dépôt `.gitlet` et les fichiers de test restent en dehors du code source :

    alias gitlet='dotnet run --project ~/gitlet_cSharp/src --'
    mkdir ~/gitlet-demo && cd ~/gitlet-demo
    gitlet init
    echo "hello" > hello.txt
    gitlet add hello.txt
    gitlet commit "add hello"
    gitlet log

## Aide-mémoire des commandes

Depuis un dossier de travail situé en dehors du code source (voir ci-dessus) :

    gitlet init
    gitlet add <nom du fichier>
    gitlet commit "<message du commit>"
    gitlet rm <nom du fichier>
    gitlet log
    gitlet global-log
    gitlet find "<message du commit>"
    gitlet branch <nom de branche>
    gitlet rm-branch <nom de branche>
    gitlet status
    gitlet checkout -- <nom du fichier>
    gitlet checkout <id du commit> -- <nom du fichier>
    gitlet checkout <nom de branche>
    gitlet reset <id du commit>


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
12. `reset <commit id>` — Moves the current branch back to a given commit and restores the working directory to match it, clearing the staging area (the id can be abbreviated).


## Quick start

### 1. Clone and build the project

    cd ~
    git clone https://github.com/JieZ-aisance/gitlet_cSharp.git
    cd gitlet_cSharp
    dotnet build src

### 2. Run the smoke test

    ./scripts/smoke_test.sh

### 3. Try Gitlet in a separate folder

    alias gitlet='dotnet run --project ~/gitlet_cSharp/src --'
    mkdir ~/gitlet-demo && cd ~/gitlet-demo
    gitlet init
    echo "hello" > hello.txt
    gitlet add hello.txt
    gitlet commit "add hello"
    gitlet log
