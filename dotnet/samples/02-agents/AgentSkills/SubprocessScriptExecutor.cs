// Copyright (c) Microsoft. All rights reserved.

// Sample subprocess-based skill script executor.
// Executes file-based skill scripts as local subprocesses.
// This is provided for demonstration purposes only.

using System.Diagnostics;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

/// <summary>
/// Executes file-based skill scripts as local subprocesses.
/// </summary>
/// <remarks>
/// This executor resolves the script's absolute path from the skill, converts
/// the arguments to CLI flags, and returns captured output. It is intended for
/// demonstration purposes only.
/// </remarks>
internal static class SubprocessScriptExecutor
{
    /// <summary>
    /// Runs a skill script as a local subprocess.
    /// </summary>
    public static Task<object?> ExecuteAsync(
        AgentSkill skill,
        AgentFileSkillScript script,
        AIFunctionArguments arguments,
        CancellationToken cancellationToken)
    {
        string scriptFullPath = skill is AgentFileSkill fileSkill
            ? Path.Combine(fileSkill.SourcePath, script.Path.Replace('/', Path.DirectorySeparatorChar))
            : script.Path;

        if (!File.Exists(scriptFullPath))
        {
            return Task.FromResult<object?>($"Error: Script file not found: {scriptFullPath}");
        }

        string extension = Path.GetExtension(scriptFullPath);
        string? interpreter = extension switch
        {
            ".py" => "python3",
            ".js" => "node",
            ".sh" => "bash",
            ".ps1" => "pwsh",
            _ => null,
        };

        var startInfo = new ProcessStartInfo
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = Path.GetDirectoryName(scriptFullPath) ?? ".",
        };

        if (interpreter is not null)
        {
            startInfo.FileName = interpreter;
            startInfo.ArgumentList.Add(scriptFullPath);
        }
        else
        {
            startInfo.FileName = scriptFullPath;
        }

        if (arguments is not null)
        {
            foreach (var (key, value) in arguments)
            {
                if (value is bool boolValue)
                {
                    if (boolValue)
                    {
                        startInfo.ArgumentList.Add(NormalizeKey(key));
                    }
                }
                else if (value is not null)
                {
                    startInfo.ArgumentList.Add(NormalizeKey(key));
                    startInfo.ArgumentList.Add(value.ToString()!);
                }
            }
        }

        try
        {
            using var process = Process.Start(startInfo);
            if (process is null)
            {
                return Task.FromResult<object?>($"Error: Failed to start process for script '{script.Name}'.");
            }

            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();

            process.WaitForExit(TimeSpan.FromSeconds(30));

            if (!string.IsNullOrEmpty(error))
            {
                output += $"\nStderr:\n{error}";
            }

            if (process.ExitCode != 0)
            {
                output += $"\nScript exited with code {process.ExitCode}";
            }

            return Task.FromResult<object?>(string.IsNullOrEmpty(output) ? "(no output)" : output.Trim());
        }
        catch (Exception ex)
        {
            return Task.FromResult<object?>($"Error: Failed to execute script '{script.Name}': {ex.Message}");
        }
    }

    /// <summary>
    /// Normalizes a parameter key to a consistent --flag format.
    /// Models may return keys with or without leading dashes (e.g., "value" vs "--value").
    /// </summary>
    private static string NormalizeKey(string key) => "--" + key.TrimStart('-');
}
