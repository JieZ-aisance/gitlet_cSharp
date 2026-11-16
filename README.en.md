# Gitlet in C#  🇬🇧

### Author: Jie ZHAO, looking avtive for a job.
### Updated: 2026-11-15

- [Introduction](#introduction)
- [Development approach](#approach)
- [Implemented commands](#commands)
- [Quick start](#quick-start)
- [References and useful links](#links)

<a id="introduction"></a>
## Introduction

Git is the version control tool developers use every day; whether for a personal project or a team project, it is indispensable. Its storage principle is based on content addressing: the bytes of a file's content are read, their hash is computed, and that hash is then used as the name under which the content is stored.
This project implements Git's core features following this [specification](https://sp21.datastructur.es/materials/proj/proj2/proj2), with GitHub Actions CI (build, tests, SonarQube Cloud analysis) on every push.


<a id="approach"></a>
## Development approach

Development took place in three phases, each on its own branch and merged through a pull request.

**Phase 1. Implement all commands and write a smoke test**

I took inspiration from the directory structure of real Git.

1. create the `main` branch, then a `dev` branch from `main` for development;
2. implement all commands according to the [specification](https://sp21.datastructur.es/materials/proj/proj2/proj2);
3. write the smoke test (`scripts/smoke_test.sh`).

**Phase 2. Unit and integration tests with AI, continuous integration and code quality**

1. create a `test` branch from `dev`;
2. write one unit test and one integration test by hand, so that Claude Code picks up my coding style, then have it generate the remaining tests, which I reviewed and adjusted;
3. configure SonarQube Cloud (code coverage, quality gate) and the GitHub Actions workflow;
4. on every push, the CI builds the project, runs the tests and launches the SonarQube analysis;
5. merge `test` into `dev` through a pull request, once the quality gate has passed.

**Phase 3. Refactoring following the SOLID principles**

1. create a `refactor` branch from `dev`;
2. extract an `ICommand` interface and split the "God class" `Repository` into dedicated classes, one per command, named `Command<CommandName>` (for example `CommandAdd`, `CommandCommit`, `CommandRm`);
3. group the helper methods into a separate class;
4. update the unit and integration tests;
5. merge `refactor` into `dev`, then `dev` into `main`, through pull requests;
6. update the README and remove the working comments on `main`.


<a id="commands"></a>
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
10. `status` — Lists the branches (the current one is marked with `*`) and the files staged for addition or removal.
11. `checkout`
    - 11.1. `checkout -- <file name>` — Restores a file to its version in the current commit.
    - 11.2. `checkout <commit id> -- <file name>` — Restores a file to its version in a given commit (the id can be abbreviated).
    - 11.3. `checkout <branch name>` — Switches to another branch and updates the working directory accordingly.
12. `reset <commit id>` — Moves the current branch and the working directory back to the state of a given commit, and clears the staging area (the id can be abbreviated).
13. `merge <branch name>` — Merges the given branch into the current branch and creates a merge commit; conflicting files are marked and reported.

<a id="quick-start"></a>
## Quick start

### 1. Clone and build the project

    cd ~
    git clone https://github.com/JieZ-aisance/gitlet_cSharp.git
    cd gitlet_cSharp
    dotnet build src
    dotnet test tests


### 2. Try Gitlet

Work in a separate folder, so that the `.gitlet` repository and the test files stay outside the source code:

    alias gitlet='dotnet run --project ~/gitlet_cSharp/src --'
    mkdir ~/gitlet-demo && cd ~/gitlet-demo
    gitlet init
    echo "hello" > hello.txt
    gitlet add hello.txt
    gitlet commit "add hello"
    gitlet log


<a id="links"></a>
## References and useful links

1. SonarQube: [quality report](https://sonarcloud.io/dashboard?id=JieZ-aisance_gitlet_cSharp)
2. Gitlet project specification: [UC Berkeley CS61B (Spring 2021), Project 2](https://sp21.datastructur.es/materials/proj/proj2/proj2)


<a id="cheat-sheet"></a>
## Command cheat sheet

From a working folder located outside the source code (see above):

    gitlet init
    gitlet add <file name>
    gitlet commit "<commit message>"
    gitlet rm <file name>
    gitlet log
    gitlet global-log
    gitlet find "<commit message>"
    gitlet branch <branch name>
    gitlet rm-branch <branch name>
    gitlet status
    gitlet checkout -- <file name>
    gitlet checkout <commit id> -- <file name>
    gitlet checkout <branch name>
    gitlet reset <commit id>
    gitlet merge <branch name>


<a id="smoke-test"></a>
## Smoke test (optional)

    ./scripts/smoke_test.sh

This script runs the main commands in a temporary folder, compares their output with the expected result, and then deletes everything it created. It should end with `Failed: 0`.