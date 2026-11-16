
# Gitlet en C#  🇫🇷

### Author: Jie ZHAO, recherche active un CDI/CDD.
### Updated: 2026-11-15

- [Gitlet en C# ](#project-gitlet)
    * [Introduction](#introduction)
    * [Démarche de développement](#demarche)
    * [Commandes implémentées](#command))
    * [Démarrage rapide](#demarrage)
    * [les références et les utiles](#utiles)

<a id="introduction"></a>
## Introduction 

Git est l'outil de gestion de versions que les développeurs utilisent au quotidien ; qu'il s'agisse d'un projet personnel ou d'un projet en équipe, il est indispensable. Son principe de stockage repose sur l'adressage par contenu : on lit les octets du contenu d'un fichier, on en calcule le hachage, puis on utilise ce hachage comme nom pour stocker ce contenu.
Ce projet implémente les fonctionnalités principales de Git en suivant ce [cahier des charges](https://sp21.datastructur.es/materials/proj/proj2/proj2), avec une CI GitHub Actions (build, tests, analyse SonarQube Cloud) à chaque push.


<a id="demarche"></a>
## Démarche de développement

Le développement s'est déroulé en trois phases, chacune sur sa propre branche et fusionnée par pull request.

**Phase 1. Implémenter toutes les commandes et écrire un test de fumée**

Je me suis inspirée de la structure de répertoires du vrai Git.

1. créer la branche `main`, puis une branche `dev` à partir de `main` pour le développement ;
2. implémenter toutes les commandes selon le [cahier des charges](https://sp21.datastructur.es/materials/proj/proj2/proj2) ;
3. écrire le test de fumée (`scripts/smoke_test.sh`).

**Phase 2. Tests unitaires et d'intégration avec l'IA, intégration continue et qualité du code**

1. créer une branche `test` à partir de `dev` ;
2. écrire à la main un test unitaire et un test d'intégration, pour que Claude Code reprenne mon style de code, puis lui faire générer les autres tests, que j'ai relus et ajustés ;
3. configurer SonarQube Cloud (couverture de code, quality gate) et le workflow GitHub Actions ;
4. à chaque push, la CI compile le projet, exécute les tests et lance l'analyse SonarQube ;
5. fusionner `test` dans `dev` par pull request, une fois la quality gate validée.

**Phase 3. Refactorisation selon les principes SOLID**

1. créer une branche `refactor` à partir de `dev` ;
2. extraire une interface `ICommand` et découper la « classe Dieu » `Repository` en classes dédiées, une par commande, nommées `Command<NomDeLaCommande>` (par exemple `CommandAdd`, `CommandCommit`, `CommandRm`) ;
3. regrouper les méthodes utilitaires dans une classe à part ;
4. adapter les tests unitaires et d'intégration ;
5. fusionner `refactor` dans `dev`, puis `dev` dans `main`, par pull request ;
6. mettre à jour le README et retirer les commentaires de travail sur `main`.


<a id="command"></a>
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
13. `merge <nom de branche>` — Fusionne la branche donnée dans la branche courante et crée un commit de fusion ; les fichiers en conflit sont marqués et signalés.

<a id="demarrage"></a>
## Démarrage rapide

### 1. Récupérer et compiler le projet

    cd ~
    git clone https://github.com/JieZ-aisance/gitlet_cSharp.git
    cd gitlet_cSharp
    dotnet test tests
    dotnet build src


### 2. Essayer Gitlet

Travaillez dans un dossier séparé, afin que le dépôt `.gitlet` et les fichiers de test restent en dehors du code source :

    alias gitlet='dotnet run --project ~/gitlet_cSharp/src --'
    mkdir ~/gitlet-demo && cd ~/gitlet-demo
    gitlet init
    echo "hello" > hello.txt
    gitlet add hello.txt
    gitlet commit "add hello"
    gitlet log


<a id="utiles"></a>
## Les les références et les utiles
1. SonarQube: https://sonarcloud.io/dashboard?id=JieZ-aisance_gitlet_cSharp
2. Gitlet project specification: https://sp21.datastructur.es/materials/proj/proj2/proj2


<a id="aide-memoire"></a>
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
    gitlet merge <nom de branche>


##  Smoke test(optionnnel) 

    ./scripts/smoke_test.sh

Ce script exécute les principales commandes dans un dossier temporaire, compare leur sortie avec le résultat attendu, puis supprime tout ce qu'il a créé. Il doit se terminer par `Failed: 0`.



