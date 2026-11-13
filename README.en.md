# Gitlet in C# 🇬🇧

### Author: Jie ZHAO
### Updated: 2026-11-02
### Gitlet project specification: https://sp21.datastructur.es/materials/proj/proj2/proj2
### 🇫🇷 The French version is available in README.md.
### 💡 For the detailed reasoning and algorithms, see the code on the `dev` branch; on the `main` branch, comments have been removed to keep the code clean.
### 💡 The unit and integration tests were first generated on the `test` branch with Claude Code (CLI), following my coding style, then reviewed and adjusted before being merged into `dev`, and then into `main`.
### 💡 Code quality: on every push and every pull request, a GitHub Actions workflow builds the project, runs the tests (xUnit), measures code coverage (Coverlet) and sends everything to SonarQube Cloud for static analysis; the quality gate (including ≥ 80% coverage on new code) is checked before each merge.

### SonarQube: https://sonarcloud.io/dashboard?id=JieZ-aisance_gitlet_cSharp

====================================================================================

## Development approach

Git is the version control tool developers use every day; whether for a personal project or a team project, it is indispensable.

Its storage principle is content addressing: the bytes of a file's content are read, a hash is computed from them, and that hash is used as the name under which the content is stored.

This project implements Git's core features, following the specification.

Development is split into phases, each with a different strategy: the first phase covers every command in the specification except `merge`; the second phase will be dedicated to `merge`.

**Phase 1.** I first designed the classes Gitlet needed, based on the directory structure of real Git. Since the commands are of moderate complexity, they are all implemented in `Repository`, with a fairly flat architecture. The development order was:

1. implement all features;
2. write the smoke test;
3. write the unit and integration tests.

**Phase 2.**  I then implemented the merge command in GitletMergeScope, following the same steps as in Phase 1.

**Phase 3.**  Once all features are complete, I plan to split Repository and refactor the project.


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
13. `merge <branch name>` — Merges the given branch into the current branch and creates a merge commit; conflicting files are marked and reported.


## Quick start

### 1. Clone and build the project

    cd ~
    git clone https://github.com/JieZ-aisance/gitlet_cSharp.git
    cd gitlet_cSharp
    dotnet test tests
    dotnet build src


### 2. Try Gitlet

Work in a separate folder, so that the `.gitlet` repository and test files stay outside the source code:

    alias gitlet='dotnet run --project ~/gitlet_cSharp/src --'
    mkdir ~/gitlet-demo && cd ~/gitlet-demo
    gitlet init
    echo "hello" > hello.txt
    gitlet add hello.txt
    gitlet commit "add hello"
    gitlet log

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


## Smoke test (optional)

    ./scripts/smoke_test.sh

This script runs the main commands in a temporary folder, compares their output with the expected result, and removes everything it created. It should end with `Failed: 0`.
