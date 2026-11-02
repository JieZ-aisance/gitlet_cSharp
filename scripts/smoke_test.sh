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
check "checkout missing"     "No such branch exists."  gitlet checkout nothing

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

echo
echo "Passed: $PASS  Failed: $FAIL"
[ "$FAIL" -eq 0 ]
