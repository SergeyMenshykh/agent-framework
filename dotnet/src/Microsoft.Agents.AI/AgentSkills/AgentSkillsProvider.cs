// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Agents.AI;

/// <summary>
/// An AI context provider that enables agents to discover and use Agent Skills from filesystem directories.
/// </summary>
/// <remarks>
/// <para>
/// Agent Skills are modular packages of instructions, scripts, and resources that extend an agent's capabilities.
/// This provider follows the progressive disclosure pattern defined by the <see href="https://agentskills.io/">Agent Skills specification</see>:
/// <list type="number">
/// <item><description><strong>Advertise</strong>: Skills are advertised via system prompt (name + description only, ~100 tokens per skill)</description></item>
/// <item><description><strong>Load</strong>: Full skill instructions loaded on-demand via <c>load_skill</c> tool (&lt;5000 tokens)</description></item>
/// <item><description><strong>Resources</strong>: Supplementary files loaded via <c>read_skill_resource</c> tool (as needed)</description></item>
/// <item><description><strong>Scripts</strong>: Executable scripts invoked via <c>run_skill_script</c> tool (as needed)</description></item>
/// </list>
/// </para>
/// <para>
/// Skills are discovered from filesystem directories containing SKILL.md files. Each skill is validated at initialization
/// to ensure all referenced resources and scripts exist. Invalid skills are excluded and warnings are logged.
/// </para>
/// </remarks>
public sealed class AgentSkillsProvider : AIContextProvider
{
    private readonly Dictionary<string, AIAgentSkill> _skills;
    private readonly ILogger<AgentSkillsProvider>? _logger;
    private readonly AITool[] _tools;
    private readonly string _stateKey;
    private readonly string _skillsInstructionPrompt;
    private readonly int _scriptTimeoutMilliseconds;

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentSkillsProvider"/> class.
    /// </summary>
    /// <param name="options">Configuration options for the provider.</param>
    /// <param name="loggerFactory">Optional logger factory.</param>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is <see langword="null"/>.</exception>
    public AgentSkillsProvider(AgentSkillsProviderOptions options, ILoggerFactory? loggerFactory = null)
    {
        _ = Throw.IfNull(options);

        this._logger = loggerFactory?.CreateLogger<AgentSkillsProvider>();
        this._stateKey = options.StateKey ?? base.StateKey;
        this._skillsInstructionPrompt = options.SkillsInstructionPrompt ?? AgentSkillsProviderOptions.DefaultSkillsInstructionPrompt;
        this._scriptTimeoutMilliseconds = options.ScriptTimeoutMilliseconds;
        this._skills = new Dictionary<string, AIAgentSkill>(StringComparer.OrdinalIgnoreCase);

        // Discover and load skills
        this.DiscoverAndLoadSkills(options.SkillDirectories);

        // Create tools
        this._tools =
        [
            AIFunctionFactory.Create(
                this.LoadSkillAsync,
                name: "load_skill",
                description: "Loads the full instructions for a specific skill."),
            AIFunctionFactory.Create(
                this.ReadSkillResourceAsync,
                name: "read_skill_resource",
                description: "Reads a supplementary resource file associated with a skill."),
            AIFunctionFactory.Create(
                this.RunSkillScriptAsync,
                name: "run_skill_script",
                description: "Executes a script associated with a skill and returns its output."),
        ];
    }

    /// <inheritdoc />
    public override string StateKey => this._stateKey;

    /// <inheritdoc />
    protected override ValueTask<AIContext> InvokingCoreAsync(InvokingContext context, CancellationToken cancellationToken = default)
    {
        var inputContext = context.AIContext;

        if (this._skills.Count == 0)
        {
            // No skills available, return input context unchanged
            return new ValueTask<AIContext>(inputContext);
        }

        // Build skills advertisement prompt
        string skillsListPrompt = this.BuildSkillsListPrompt();
        string fullPrompt = string.Format(this._skillsInstructionPrompt, skillsListPrompt);

        // Add skills instruction to the context
        var skillsMessage = new ChatMessage(ChatRole.System, fullPrompt)
            .WithAgentRequestMessageSource(AgentRequestMessageSourceType.AIContextProvider, this.GetType().FullName!);

        return new ValueTask<AIContext>(new AIContext
        {
            Instructions = inputContext.Instructions,
            Messages = (inputContext.Messages ?? []).Prepend(skillsMessage),
            Tools = (inputContext.Tools ?? []).Concat(this._tools)
        });
    }

    /// <summary>
    /// Loads the full instructions for a specific skill.
    /// </summary>
    /// <param name="skillName">The name of the skill to load.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The full skill instructions as markdown text.</returns>
    internal async Task<string> LoadSkillAsync(string skillName, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask.ConfigureAwait(false); // Make async for consistency with other tools

        if (string.IsNullOrWhiteSpace(skillName))
        {
            return "Error: Skill name cannot be empty.";
        }

        if (!this._skills.TryGetValue(skillName, out AIAgentSkill? skill))
        {
            return $"Error: Skill '{skillName}' not found. Available skills: {string.Join(", ", this._skills.Keys)}";
        }

        if (this._logger?.IsEnabled(LogLevel.Information) == true)
        {
            this._logger.LogInformation("Loading skill: {SkillName}", skillName);
        }

        // Format the skill content with metadata
        var sb = new StringBuilder();
        sb.AppendLine($"# Skill: {skill.Name}");
        sb.AppendLine($"**Description:** {skill.Description}");
        if (!string.IsNullOrWhiteSpace(skill.SourcePath))
        {
            sb.AppendLine($"**Path:** {skill.SourcePath}");
        }
        sb.AppendLine();

        if (skill.Resources.Count > 0)
        {
            sb.AppendLine("**Available Resources:**");
            foreach (var resource in skill.Resources)
            {
                sb.AppendLine($"- {resource.Name}");
            }
            sb.AppendLine();
        }

        if (skill.Scripts.Count > 0)
        {
            sb.AppendLine("**Available Scripts:**");
            foreach (var script in skill.Scripts)
            {
                sb.AppendLine($"- {script.Name}");
            }
            sb.AppendLine();
        }

        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine(skill.Content);

        return sb.ToString();
    }

    /// <summary>
    /// Reads a supplementary resource file associated with a skill.
    /// </summary>
    /// <param name="skillName">The name of the skill.</param>
    /// <param name="resourceName">The name of the resource file to read.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The content of the resource file.</returns>
    internal async Task<string> ReadSkillResourceAsync(string skillName, string resourceName, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(skillName))
        {
            return "Error: Skill name cannot be empty.";
        }

        if (string.IsNullOrWhiteSpace(resourceName))
        {
            return "Error: Resource name cannot be empty.";
        }

        if (!this._skills.TryGetValue(skillName, out AIAgentSkill? skill))
        {
            return $"Error: Skill '{skillName}' not found.";
        }

        var resource = skill.Resources.FirstOrDefault(r => r.Name.Equals(resourceName, StringComparison.OrdinalIgnoreCase));
        if (resource is null)
        {
            string availableResources = skill.Resources.Count > 0
                ? string.Join(", ", skill.Resources.Select(r => r.Name))
                : "none";
            return $"Error: Resource '{resourceName}' not found in skill '{skillName}'. Available resources: {availableResources}";
        }

        if (this._logger?.IsEnabled(LogLevel.Information) == true)
        {
            this._logger.LogInformation("Reading resource '{ResourceName}' from skill '{SkillName}'", resourceName, skillName);
        }

        return resource.Content;
    }

    /// <summary>
    /// Executes a script associated with a skill and returns its output.
    /// </summary>
    /// <param name="skillName">The name of the skill.</param>
    /// <param name="scriptName">The name of the script to execute.</param>
    /// <param name="args">Command-line arguments to pass to the script.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The output from the script execution.</returns>
    internal async Task<string> RunSkillScriptAsync(
        string skillName,
        string scriptName,
        string[] args,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(skillName))
        {
            return "Error: Skill name cannot be empty.";
        }

        if (string.IsNullOrWhiteSpace(scriptName))
        {
            return "Error: Script name cannot be empty.";
        }

        if (!this._skills.TryGetValue(skillName, out AIAgentSkill? skill))
        {
            return $"Error: Skill '{skillName}' not found.";
        }

        var script = skill.Scripts.FirstOrDefault(s => s.Name.Equals(scriptName, StringComparison.OrdinalIgnoreCase));
        if (script is null)
        {
            string availableScripts = skill.Scripts.Count > 0
                ? string.Join(", ", skill.Scripts.Select(s => s.Name))
                : "none";
            return $"Error: Script '{scriptName}' not found in skill '{skillName}'. Available scripts: {availableScripts}";
        }

        if (this._logger?.IsEnabled(LogLevel.Information) == true)
        {
            this._logger.LogInformation("Executing script '{ScriptName}' from skill '{SkillName}'", scriptName, skillName);
        }

        try
        {
            return await this.ExecuteScriptAsync(script.Path, args, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            this._logger?.LogError(ex, "Failed to execute script '{ScriptName}' from skill '{SkillName}'", scriptName, skillName);
            return $"Error executing script: {ex.Message}";
        }
    }

    private void DiscoverAndLoadSkills(IEnumerable<string> skillDirectories)
    {
        var discoveredPaths = SkillDiscovery.DiscoverSkills(skillDirectories);

        if (this._logger?.IsEnabled(LogLevel.Information) == true)
        {
            this._logger.LogInformation("Discovered {Count} potential skills", discoveredPaths.Count);
        }

        foreach (string skillPath in discoveredPaths)
        {
            try
            {
                string skillFilePath = SkillDiscovery.GetSkillFilePath(skillPath);
                var validationResult = SkillValidator.ValidateFromFile(skillFilePath, skillPath);

                if (!validationResult.IsValid)
                {
                    this._logger?.LogWarning(
                        "Excluding skill at '{SkillPath}': {Errors}",
                        skillPath,
                        validationResult.GetErrorMessage());
                    continue;
                }

                AIAgentSkill skill = SkillMarkdownParser.Parse(skillFilePath, skillPath);
                this._skills[skill.Name] = skill;

                if (this._logger?.IsEnabled(LogLevel.Information) == true)
                {
                    this._logger.LogInformation("Loaded skill: {SkillName}", skill.Name);
                }
            }
            catch (Exception ex)
            {
                this._logger?.LogWarning(ex, "Failed to load skill from '{SkillPath}'", skillPath);
            }
        }

        if (this._logger?.IsEnabled(LogLevel.Information) == true)
        {
            this._logger.LogInformation("Successfully loaded {Count} skills", this._skills.Count);
        }
    }

    private string BuildSkillsListPrompt()
    {
        var sb = new StringBuilder();
        foreach (var skill in this._skills.Values.OrderBy(s => s.Name))
        {
            sb.AppendLine($"- **{skill.Name}**: {skill.Description}");
        }
        return sb.ToString().TrimEnd();
    }

    private async Task<string> ExecuteScriptAsync(string scriptPath, string[] args, CancellationToken cancellationToken)
    {
        // Validate script path to prevent path traversal
        string fullScriptPath = Path.GetFullPath(scriptPath);
        if (!File.Exists(fullScriptPath))
        {
            throw new FileNotFoundException($"Script file not found: {fullScriptPath}");
        }

        // Determine the shell and command based on the platform and file extension
        string fileName;
        string arguments;

        string extension = Path.GetExtension(fullScriptPath).ToUpperInvariant();

#if NET5_0_OR_GREATER
        if (OperatingSystem.IsWindows())
#else
        if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
#endif
        {
            if (extension == ".PS1")
            {
                fileName = "powershell.exe";
                arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{fullScriptPath}\"";
            }
            else if (extension == ".CMD" || extension == ".BAT")
            {
                fileName = "cmd.exe";
                arguments = $"/c \"{fullScriptPath}\"";
            }
            else
            {
                // Assume it's a PowerShell script
                fileName = "powershell.exe";
                arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{fullScriptPath}\"";
            }
        }
        else
        {
            // Linux/Mac - use bash
            fileName = "/bin/bash";
            arguments = $"\"{fullScriptPath}\"";
        }

        // Append user-provided arguments
        if (args?.Length > 0)
        {
            arguments += " " + string.Join(" ", args.Select(arg => $"\"{arg}\""));
        }

        var processStartInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = Path.GetDirectoryName(fullScriptPath) ?? Environment.CurrentDirectory
        };

        using var process = new Process { StartInfo = processStartInfo };
        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();

        process.OutputDataReceived += (sender, e) =>
        {
            if (e.Data is not null)
            {
                outputBuilder.AppendLine(e.Data);
            }
        };

        process.ErrorDataReceived += (sender, e) =>
        {
            if (e.Data is not null)
            {
                errorBuilder.AppendLine(e.Data);
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        // Wait for the process with timeout
        bool completed;
        if (this._scriptTimeoutMilliseconds > 0)
        {
#if NET5_0_OR_GREATER
            completed = await process.WaitForExitAsync(TimeSpan.FromMilliseconds(this._scriptTimeoutMilliseconds), cancellationToken).ConfigureAwait(false);
#else
            var tcs = new TaskCompletionSource<bool>();
            using (cancellationToken.Register(() => tcs.TrySetCanceled()))
            {
                var timeoutTask = Task.Delay(this._scriptTimeoutMilliseconds, cancellationToken);
                var exitTask = Task.Run(() =>
                {
                    process.WaitForExit();
                    return true;
                });

                var completedTask = await Task.WhenAny(exitTask, timeoutTask).ConfigureAwait(false);
                completed = completedTask == exitTask;
            }
#endif
        }
        else
        {
#if NET5_0_OR_GREATER
            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
#else
            await Task.Run(() => process.WaitForExit(), cancellationToken).ConfigureAwait(false);
#endif
            completed = true;
        }

        if (!completed)
        {
            try
            {
#if NET5_0_OR_GREATER
                process.Kill(entireProcessTree: true);
#else
                process.Kill();
#endif
            }
            catch
            {
                // Ignore errors when killing the process
            }
            throw new TimeoutException($"Script execution timed out after {this._scriptTimeoutMilliseconds}ms");
        }

        string output = outputBuilder.ToString();
        string error = errorBuilder.ToString();

        if (process.ExitCode != 0)
        {
            string errorMessage = !string.IsNullOrWhiteSpace(error) ? error : "Script execution failed";
            throw new InvalidOperationException($"Script exited with code {process.ExitCode}: {errorMessage}");
        }

        return !string.IsNullOrWhiteSpace(output) ? output : error;
    }
}

#if NET5_0_OR_GREATER
// Extension method for Process.WaitForExitAsync with timeout
internal static class ProcessExtensions
{
    public static async Task<bool> WaitForExitAsync(this Process process, TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(timeout);

        try
        {
            await process.WaitForExitAsync(cts.Token).ConfigureAwait(false);
            return true;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return false; // Timeout
        }
    }
}
#endif
