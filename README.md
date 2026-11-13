# Gitlet en C#  🇫🇷

### Author: Jie ZHAO
### Updated: 2026-11-13
### Cahier des charges du projet Gitlet: https://sp21.datastructur.es/materials/proj/proj2/proj2
### 🇬🇧 English version is available in README.en.md.
### 💡Pour le détail du raisonnement et des algorithmes, consultez le code de la branche `dev` ; sur la branche `main`, les commentaires ont été retirés pour garder un code propre.
### 💡Les tests unitaires et d'intégration ont d'abord été générés sur la branche `test` avec Claude Code (CLI), en respectant mon style de code, puis relus et ajustés avant d'être fusionnés dans `dev`, et ensuite dans `main`.
### 💡 Qualité du code : à chaque push et à chaque pull request, un workflow GitHub Actions compile le projet, exécute les tests (xUnit), mesure la couverture de code (Coverlet) et envoie le tout à SonarQube Cloud pour l'analyse statique ; la quality gate (notamment ≥ 80 % de couverture sur le nouveau code) est vérifiée avant chaque fusion.

### SonarQube: https://sonarcloud.io/dashboard?id=JieZ-aisance_gitlet_cShar
### repos sur github: https://github.com/JieZ-aisance/gitlet_cSharp

====================================================================================

## Réflexion et Démarche de développement

Git est l'outil de gestion de versions que les développeurs utilisent au quotidien ; qu'il s'agisse d'un projet personnel ou d'un projet en équipe, il est indispensable.

Son principe de stockage repose sur l'adressage par contenu : on lit les octets du contenu d'un fichier, on en calcule le hachage, puis on utilise ce hachage comme nom pour stocker ce contenu.

Ce projet implémente les fonctionnalités principales de Git en suivant le cahier des charges.

Le développement se déroule en deux phases, avec des stratégies différentes : la première phase couvre toutes les commandes du cahier des charges à l'exception de `merge` ; la seconde phase sera consacrée à `merge`.

**Phase 1.** J'ai d'abord conçu les classes nécessaires à Gitlet en m'inspirant de la structure de répertoires du vrai Git. Les commandes étant de complexité modérée, elles sont toutes implémentées dans `Repository`, avec une architecture plutôt plate. L'ordre de développement a été le suivant :

1. implémenter toutes les fonctionnalités ;
2. écrire le test de fumée (smoke test) ;
3. écrire les tests unitaires et d'intégration.

**Phase 2.** J'ai ensuite implémenté la commande "merge" dans "GitletMergeScope" en suivant les étapes identiques que la phase 1.

**Phase 3.**  Une fois toutes les fonctionnalités terminées, je prévois de découper `Repository` et refecto le projet. 



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



