#!/usr/bin/env bash
# Gitlet 冒烟测试:在临时目录里跑一遍主要命令,比对输出,结束后自动清理

PROJECT="$(cd "$(dirname "$0")/.." && pwd)/src"

# 先编译一次,后面用 --no-build 加快速度
dotnet build "$PROJECT" -v q > /dev/null || { echo "Build failed"; exit 1; }

# 临时目录,脚本结束时(无论成功失败)自动删除
WORK="$(mktemp -d)"
trap 'rm -rf "$WORK"' EXIT
cd "$WORK"

gitlet() { dotnet run --no-build --project "$PROJECT" -- "$@"; }

# 文件是否存在:输出 yes / no
exists() { [ -f "$1" ] && echo yes || echo no; }

# log 里包含某段文字的次数
log_has() { gitlet log | grep -c "$1"; }

PASS=0
FAIL=0

# 用法:check "说明" "预期输出" 命令...
check() {
    local desc="$1" expected="$2"
    shift 2
    local actual
    actual="$("$@" 2>&1)"
    if [ "$actual" == "$expected" ]; then
        PASS=$((PASS + 1))
        echo "  ok   $desc"
    else
        FAIL=$((FAIL + 1))
        echo "  FAIL $desc"
        echo "       expected: $expected"
        echo "       actual:   $actual"
    fi
}

echo "== init"
check "init succeeds"        ""  gitlet init
check "init twice fails"     "A Gitlet version-control system already exists in the current directory."  gitlet init

echo "== add / commit"
echo "hello" > hello.txt
check "add file"             ""  gitlet add hello.txt
check "add missing file"     "File does not exist."  gitlet add nothing.txt
check "commit"               ""  gitlet commit "add hello"
check "commit nothing"       "No changes added to the commit."  gitlet commit "empty"

echo "== rm"
check "rm unrelated file"    "No reason to remove the file."  gitlet rm nothing.txt

echo "== branch / checkout"
check "create branch"        ""  gitlet branch dev
check "branch exists"        "A branch with that name already exists."  gitlet branch dev
check "checkout current"     "No need to checkout the current branch."  gitlet checkout main
check "checkout missing"     "A branch with that name does not exist."  gitlet checkout nothing

echo "== checkout file"
echo "changed" > hello.txt
check "restore file"         ""  gitlet checkout -- hello.txt
check "content restored"     "hello"  cat hello.txt

echo "== find"
check "find missing"         "Found no commit with that message."  gitlet find "nothing"

echo "== global errors"
check "no command"           "Please enter a command."  gitlet
check "unknown command"      "No command with that name exists."  gitlet hello
check "missing operand"      "Incorrect operands."  gitlet add

echo "== merge: failure cases"
mkdir "$WORK/m1" && cd "$WORK/m1"
gitlet init
echo "a" > a.txt && gitlet add a.txt && gitlet commit "add a"

check "merge missing branch"   "A branch with that name does not exist."  gitlet merge nothing
check "merge with itself"      "Cannot merge a branch with itself."  gitlet merge main

gitlet branch other
echo "x" > x.txt && gitlet add x.txt
check "uncommitted changes"    "You have uncommitted changes."  gitlet merge other
gitlet rm x.txt && rm -f x.txt          # 取消暂存并删掉,恢复干净状态

echo "== merge: early exits"
echo "b" > b.txt && gitlet add b.txt && gitlet commit "add b on main"
check "given is ancestor"      "Given branch is an ancestor of the current branch."  gitlet merge other

gitlet checkout other
check "fast-forward"           "Current branch fast-forwarded."  gitlet merge main
check "fast-forward brings file"  "b"  cat b.txt

echo "== merge: rules"
mkdir "$WORK/m2" && cd "$WORK/m2"
gitlet init
echo "v1" > a.txt && echo "keep" > d.txt
gitlet add a.txt && gitlet add d.txt && gitlet commit "split"

gitlet branch other
gitlet checkout other
echo "v2" > a.txt && gitlet add a.txt   # 规则 1:给定分支修改
echo "new" > c.txt && gitlet add c.txt  # 规则 5:给定分支新增
gitlet rm d.txt                          # 规则 6:给定分支删除
gitlet commit "changes on other"

gitlet checkout main
echo "main" > b.txt && gitlet add b.txt && gitlet commit "change on main"   # 规则 4

check "merge succeeds"         ""     gitlet merge other
check "rule 1: modified in given"  "v2"    cat a.txt
check "rule 4: added in current"   "main"  cat b.txt
check "rule 5: added in given"     "new"   cat c.txt
check "rule 6: removed in given"   "no"    exists d.txt
check "merge commit in log"    "1"    log_has "Merged other into main."

echo "== merge: conflict"
mkdir "$WORK/m3" && cd "$WORK/m3"
gitlet init
echo "v1" > f.txt && gitlet add f.txt && gitlet commit "split"
gitlet branch other
gitlet checkout other
echo "v2" > f.txt && gitlet add f.txt && gitlet commit "change on other"
gitlet checkout main
echo "v3" > f.txt && gitlet add f.txt && gitlet commit "change on main"

check "conflict message"       "Encountered a merge conflict."  gitlet merge other
check "conflict file content"  $'<<<<<<< HEAD\nv3\n=======\nv2\n>>>>>>>'  cat f.txt
check "conflict still commits" "1"    log_has "Merged other into main."

echo "== merge: untracked file in the way"
mkdir "$WORK/m4" && cd "$WORK/m4"
gitlet init
echo "a" > a.txt && gitlet add a.txt && gitlet commit "split"
gitlet branch other
gitlet checkout other
echo "theirs" > u.txt && gitlet add u.txt && gitlet commit "add u on other"
gitlet checkout main
echo "b" > b.txt && gitlet add b.txt && gitlet commit "change on main"
echo "mine" > u.txt                      # 未跟踪,而给定分支有同名文件

check "untracked in the way"   "There is an untracked file in the way; delete it, or add and commit it first."  gitlet merge other
check "untracked file kept"    "mine"  cat u.txt

cd "$WORK"

echo
echo "Passed: $PASS  Failed: $FAIL"
[ "$FAIL" -eq 0 ]
