// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Agents.AI;

/// <summary>
/// An <see cref="AIContextProvider"/> that exposes agent skills from one or more <see cref="AgentSkillsSource"/> instances.
/// </summary>
/// <remarks>
/// <para>
/// This provider implements the progressive disclosure pattern from the
/// <see href="https://agentskills.io/">Agent Skills specification</see>:
/// </para>
/// <list type="number">
/// <item><description><strong>Advertise</strong> — skill names and descriptions are injected into the system prompt.</description></item>
/// <item><description><strong>Load</strong> — the full skill body is returned via the <c>load_skill</c> tool.</description></item>
/// <item><description><strong>Read resources</strong> — supplementary content is read on demand via the <c>read_skill_resource</c> tool.</description></item>
/// <item><description><strong>Run scripts</strong> — scripts are executed via the <c>run_skill_script</c> tool (when scripts exist).</description></item>
/// </list>
/// </remarks>
[Experimental(DiagnosticIds.Experiments.AgentsAIExperiments)]
public sealed partial class AgentSkillsProvider : AIContextProvider, IDisposable
{
    private const string DefaultSkillsInstructionPrompt =
        """
        You have access to skills containing domain-specific knowledge and capabilities.
        Each skill provides specialized instructions, reference documents, and assets for specific tasks.

        <available_skills>
        {0}
        </available_skills>

        When a task aligns with a skill's domain, follow these steps in exact order:
        - Use `load_skill` to retrieve the skill's instructions
        - Follow the provided guidance
        - Use `read_skill_resource` to read any referenced resources, using the name exactly as listed
           (e.g. `"style-guide"` not `"style-guide.md"`, `"references/FAQ.md"` not `"FAQ.md"`).
        {1}
        Only load what is needed, when it is needed.
        """;

    private readonly AgentSkillsSource _source;
    private readonly AgentSkillsProviderOptions? _options;
    private readonly ILogger<AgentSkillsProvider> _logger;
    private readonly SemaphoreSlim _skillsInitLock = new(1, 1);
    private IReadOnlyList<AgentSkill>? _skills;
    private IEnumerable<AITool>? _tools;
    private string? _instructions;

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentSkillsProvider"/> class.
    /// </summary>
    /// <param name="source">The skill source providing skills.</param>
    /// <param name="options">Optional configuration.</param>
    /// <param name="loggerFactory">Optional logger factory.</param>
    public AgentSkillsProvider(AgentSkillsSource source, AgentSkillsProviderOptions? options = null, ILoggerFactory? loggerFactory = null)
    {
        this._source = Throw.IfNull(source);
        this._options = options;
        this._logger = (loggerFactory ?? NullLoggerFactory.Instance).CreateLogger<AgentSkillsProvider>();
    }

    /// <inheritdoc />
    protected override async ValueTask<AIContext> ProvideAIContextAsync(InvokingContext context, CancellationToken cancellationToken = default)
    {
        this._skills ??= await this.LoadSkillsAsync(cancellationToken).ConfigureAwait(false);

        if (this._skills is not { Count: > 0 })
        {
            return await base.ProvideAIContextAsync(context, cancellationToken).ConfigureAwait(false);
        }

        return new AIContext
        {
            Instructions = this._instructions ??= this.BuildSkillsInstructions(),
            Tools = this._tools ??= this.BuildSkillsTools()
        };
    }

    private async Task<IReadOnlyList<AgentSkill>> LoadSkillsAsync(CancellationToken cancellationToken)
    {
        await this._skillsInitLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await this._source.GetSkillsAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            this._skillsInitLock.Release();
        }
    }

    private string? BuildSkillsInstructions()
    {
        if (this._skills is not { Count: > 0 })
        {
            return null;
        }

        string promptTemplate = this._options?.SkillsInstructionPrompt ?? DefaultSkillsInstructionPrompt;

        var sb = new StringBuilder();
        foreach (var skill in this._skills.OrderBy(s => s.Frontmatter.Name, StringComparer.Ordinal))
        {
            sb.AppendLine("  <skill>");
            sb.AppendLine($"    <name>{SecurityElement.Escape(skill.Frontmatter.Name)}</name>");
            sb.AppendLine($"    <description>{SecurityElement.Escape(skill.Frontmatter.Description)}</description>");
            sb.AppendLine("  </skill>");
        }

        string scriptInstruction = string.Empty;
        foreach (var skill in this._skills)
        {
            if (skill.Scripts is { Count: > 0 })
            {
                scriptInstruction = "- Use `run_skill_script` to run referenced scripts, using the name exactly as listed\n";
                break;
            }
        }

        return string.Format(promptTemplate, sb.ToString().TrimEnd(), scriptInstruction);
    }

    private List<AITool>? BuildSkillsTools()
    {
        if (this._skills is not { Count: > 0 })
        {
            return null;
        }

        var tools = new List<AITool>
        {
            AIFunctionFactory.Create(
                this.LoadSkill,
                name: "load_skill",
                description: "Loads the full content of a specific skill"),
            AIFunctionFactory.Create(
                this.ReadSkillResourceAsync,
                name: "read_skill_resource",
                description: "Reads a resource associated with a skill, such as references, assets, or dynamic data."),
        };

        // Add run_skill_script tool if any skill has scripts
        if (this._skills.Any(skill => skill.Scripts?.Any() ?? false))
        {
            AIFunction scriptFunction = AIFunctionFactory.Create(
                this.RunSkillScriptAsync,
                name: "run_skill_script",
                description: "Runs a script associated with a skill.");

            tools.Add(this._options?.ScriptApproval == true
                ? new ApprovalRequiredAIFunction(scriptFunction)
                : scriptFunction);
        }

        return tools;
    }

    private string LoadSkill(string skillName)
    {
        if (string.IsNullOrWhiteSpace(skillName))
        {
            return "Error: Skill name cannot be empty.";
        }

        var skill = this._skills?.FirstOrDefault(skill => skill.Frontmatter.Name == skillName);
        if (skill == null)
        {
            return $"Error: Skill '{skillName}' not found.";
        }

        LogSkillLoading(this._logger, skillName);

        return skill.Content;
    }

    private async Task<object?> ReadSkillResourceAsync(string skillName, string resourceName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(skillName))
        {
            return "Error: Skill name cannot be empty.";
        }

        if (string.IsNullOrWhiteSpace(resourceName))
        {
            return "Error: Resource name cannot be empty.";
        }

        var skill = this._skills?.FirstOrDefault(skill => skill.Frontmatter.Name == skillName);
        if (skill == null)
        {
            return $"Error: Skill '{skillName}' not found.";
        }

        var resource = skill.Resources?.FirstOrDefault(resource => resource.Name == resourceName);
        if (resource is null)
        {
            return $"Error: Resource '{resourceName}' not found in skill '{skillName}'.";
        }

        try
        {
            return await resource.ReadAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            LogResourceReadError(this._logger, skillName, resourceName, ex);
            return $"Error: Failed to read resource '{resourceName}' from skill '{skillName}'.";
        }
    }

    private async Task<object?> RunSkillScriptAsync(string skillName, string scriptName, IDictionary<string, object?>? arguments, IServiceProvider? serviceProvider, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(skillName))
        {
            return "Error: Skill name cannot be empty.";
        }

        if (string.IsNullOrWhiteSpace(scriptName))
        {
            return "Error: Script name cannot be empty.";
        }

        var skill = this._skills?.FirstOrDefault(skill => skill.Frontmatter.Name == skillName); if (skill == null)
        {
            return $"Error: Skill '{skillName}' not found.";
        }

        var script = skill.Scripts?.FirstOrDefault(resource => resource.Name == scriptName);
        if (script is null)
        {
            return $"Error: Script '{scriptName}' not found in skill '{skillName}'.";
        }

        try
        {
            return await script.ExecuteAsync(skill, new AIFunctionArguments(arguments) { Services = serviceProvider }, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            LogScriptExecutionError(this._logger, skillName, scriptName, ex);
            return $"Error: Failed to execute script '{scriptName}' from skill '{skillName}'.";
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        this._skillsInitLock.Dispose();
    }

    [LoggerMessage(LogLevel.Information, "Loading skill: {SkillName}")]
    private static partial void LogSkillLoading(ILogger logger, string skillName);

    [LoggerMessage(LogLevel.Error, "Failed to read resource '{ResourceName}' from skill '{SkillName}'")]
    private static partial void LogResourceReadError(ILogger logger, string skillName, string resourceName, Exception exception);

    [LoggerMessage(LogLevel.Error, "Failed to execute script '{ScriptName}' from skill '{SkillName}'")]
    private static partial void LogScriptExecutionError(ILogger logger, string skillName, string scriptName, Exception exception);
}
