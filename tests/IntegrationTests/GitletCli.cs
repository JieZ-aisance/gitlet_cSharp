using System.Diagnostics;

namespace Gitlet.Tests.IntegrationTests;

public sealed record CliResult(int ExitCode, string Output, string Error);

// 在指定目录里启动真正的 Gitlet 程序(dotnet Gitlet.dll <参数>),返回它的输出。
// Gitlet.dll 通过项目引用被复制到测试的输出目录里。
public static class GitletCli
{
    private static readonly string GitletDll = Path.Combine(AppContext.BaseDirectory, "Gitlet.dll");

    public static CliResult Run(string workingDirectory, params string[] args)
    {
        var startInfo = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        startInfo.ArgumentList.Add(GitletDll);
        foreach (var arg in args)
        {
            startInfo.ArgumentList.Add(arg);
        }

        // 固定时区,让 log 里的日期不依赖本机设置
        startInfo.Environment["TZ"] = "UTC";

        using var process = Process.Start(startInfo)!;

        // 两个流都要异步读,否则输出多时子进程会被卡住
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();

        if (!process.WaitForExit(TimeSpan.FromSeconds(30)))
        {
            process.Kill(entireProcessTree: true);
            throw new TimeoutException($"gitlet {string.Join(' ', args)} did not finish in 30s");
        }

        return new CliResult(process.ExitCode, output.Result, error.Result);
    }
}
