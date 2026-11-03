using System.Diagnostics;

namespace Gitlet.Tests.IntegrationTests;

public sealed record CliResult(int ExitCode, string Output, string Error);

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

        startInfo.Environment["TZ"] = "UTC";

        using var process = Process.Start(startInfo)!;
        
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
