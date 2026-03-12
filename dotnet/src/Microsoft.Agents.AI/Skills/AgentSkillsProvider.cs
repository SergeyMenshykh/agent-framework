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
public sealed partial class AgentSkillsProvider : AIContextProvider
{
    private const string DefaultSkillsInstructionPrompt =
        """
        You have access to skills containing domain-specific knowledge and capabilities.
        Each skill provides specialized instructions, reference documents, and assets for specific tasks.

        <available_skills>
        {0}
        </available_skills>

        When a task aligns with a skill's domain:
        1. Use `load_skill` to retrieve the skill's instructions
        2. Follow the provided guidance
        3. Use `read_skill_resource` to read any references or other files mentioned by the skill
        {1}
        Only load what is needed, when it is needed.
        """;

    private readonly AgentSkillsSource _source;
    private readonly AgentSkillsProviderOptions? _options;
    private readonly ILogger<AgentSkillsProvider> _logger;

    private Dictionary<string, AgentSkill>? _skillsByName;
    private List<AgentSkill>? _skills;

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
        await this.EnsureSkillsLoadedAsync(cancellationToken).ConfigureAwait(false);

        if (this._skills!.Count == 0)
        {
            return await base.ProvideAIContextAsync(context, cancellationToken).ConfigureAwait(false);
        }

        string? instructions = this.BuildSkillsInstructionPrompt();
        var tools = new List<AITool>
        {
            AIFunctionFactory.Create(
                this.LoadSkill,
                name: "load_skill",
                description: "Loads the full instructions for a specific skill."),
            AIFunctionFactory.Create(
                this.ReadSkillResourceAsync,
                name: "read_skill_resource",
                description: "Reads a file associated with a skill, such as references or assets."),
        };

        // Add run_skill_script tool if any skill has scripts (R5.5)
        bool hasScripts = false;
        foreach (var skill in this._skills)
        {
            if (skill.Scripts.Count > 0)
            {
                hasScripts = true;
                break;
            }
        }

        if (hasScripts)
        {
            AIFunction scriptFunction = AIFunctionFactory.Create(
                this.RunSkillScriptAsync,
                name: "run_skill_script",
                description: "Executes a script associated with a skill.");

            tools.Add(this._options?.ScriptApproval == true
                ? new ApprovalRequiredAIFunction(scriptFunction)
                : scriptFunction);
        }

        return new AIContext
        {
            Instructions = instructions,
            Tools = tools
        };
    }

    private string LoadSkill(string skillName)
    {
        if (string.IsNullOrWhiteSpace(skillName))
        {
            return "Error: Skill name cannot be empty.";
        }

        if (this._skillsByName == null || !this._skillsByName.TryGetValue(skillName, out AgentSkill? skill))
        {
            return $"Error: Skill '{skillName}' not found.";
        }

        LogSkillLoading(this._logger, skillName);

        return skill.Body;
    }

    private async Task<string> ReadSkillResourceAsync(string skillName, string resourceName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(skillName))
        {
            return "Error: Skill name cannot be empty.";
        }

        if (string.IsNullOrWhiteSpace(resourceName))
        {
            return "Error: Resource name cannot be empty.";
        }

        if (this._skillsByName == null || !this._skillsByName.TryGetValue(skillName, out AgentSkill? skill))
        {
            return $"Error: Skill '{skillName}' not found.";
        }

        try
        {
            AgentSkillResource? resource = null;
            foreach (var r in skill.Resources)
            {
                if (string.Equals(r.Name, resourceName, StringComparison.OrdinalIgnoreCase))
                {
                    resource = r;
                    break;
                }
            }

            if (resource == null)
            {
                return $"Error: Resource '{resourceName}' not found in skill '{skillName}'.";
            }

            return await resource.ReadAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            LogResourceReadError(this._logger, skillName, resourceName, ex);
            return $"Error: Failed to read resource '{resourceName}' from skill '{skillName}'.";
        }
    }

    private async Task<string> RunSkillScriptAsync(string skillName, string scriptName, string? argumentsJson, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(skillName))
        {
            return "Error: Skill name cannot be empty.";
        }

        if (string.IsNullOrWhiteSpace(scriptName))
        {
            return "Error: Script name cannot be empty.";
        }

        if (this._skillsByName == null || !this._skillsByName.TryGetValue(skillName, out AgentSkill? skill))
        {
            return $"Error: Skill '{skillName}' not found.";
        }

        AgentSkillScript? script = null;
        foreach (var s in skill.Scripts)
        {
            if (string.Equals(s.Name, scriptName, System.StringComparison.OrdinalIgnoreCase))
            {
                script = s;
                break;
            }
        }

        if (script == null)
        {
            return $"Error: Script '{scriptName}' not found in skill '{skillName}'.";
        }

        var arguments = new Dictionary<string, object?>();
        if (!string.IsNullOrWhiteSpace(argumentsJson))
        {
            // MVP: simple key-value parsing. Full JSON deserialization can be added later.
            arguments["raw"] = argumentsJson;
        }

        try
        {
            return await script.ExecuteAsync(arguments, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            LogScriptExecutionError(this._logger, skillName, scriptName, ex);
            return $"Error: Failed to execute script '{scriptName}' from skill '{skillName}'.";
        }
    }

    private async Task EnsureSkillsLoadedAsync(CancellationToken cancellationToken)
    {
        if (this._skills != null)
        {
            return;
        }

        IReadOnlyList<AgentSkill> allSkills = await this._source.GetSkillsAsync(cancellationToken).ConfigureAwait(false);

        // Apply global filter (R7.3)
        List<AgentSkill> filtered;
        if (this._options?.Filter != null)
        {
            filtered = new List<AgentSkill>();
            foreach (var skill in allSkills)
            {
                if (this._options.Filter(skill))
                {
                    filtered.Add(skill);
                }
            }
        }
        else
        {
            filtered = new List<AgentSkill>(allSkills);
        }

        var byName = new Dictionary<string, AgentSkill>(StringComparer.OrdinalIgnoreCase);
        foreach (var skill in filtered)
        {
            byName[skill.Name] = skill;
        }

        this._skillsByName = byName;
        this._skills = filtered;
    }

    private string? BuildSkillsInstructionPrompt()
    {
        if (this._skills == null || this._skills.Count == 0)
        {
            return null;
        }

        string promptTemplate = this._options?.SkillsInstructionPrompt ?? DefaultSkillsInstructionPrompt;

        var sb = new StringBuilder();
        foreach (var skill in this._skills.OrderBy(s => s.Name, StringComparer.Ordinal))
        {
            sb.AppendLine("  <skill>");
            sb.AppendLine($"    <name>{SecurityElement.Escape(skill.Name)}</name>");
            sb.AppendLine($"    <description>{SecurityElement.Escape(skill.Description)}</description>");
            sb.AppendLine("  </skill>");
        }

        bool hasScripts = false;
        foreach (var skill in this._skills)
        {
            if (skill.Scripts.Count > 0)
            {
                hasScripts = true;
                break;
            }
        }

        string scriptInstruction = hasScripts
            ? "4. Use `run_skill_script` to execute scripts when needed\n"
            : string.Empty;

        return string.Format(promptTemplate, sb.ToString().TrimEnd(), scriptInstruction);
    }

    [LoggerMessage(LogLevel.Information, "Loading skill: {SkillName}")]
    private static partial void LogSkillLoading(ILogger logger, string skillName);

    [LoggerMessage(LogLevel.Error, "Failed to read resource '{ResourceName}' from skill '{SkillName}'")]
    private static partial void LogResourceReadError(ILogger logger, string skillName, string resourceName, Exception exception);

    [LoggerMessage(LogLevel.Error, "Failed to execute script '{ScriptName}' from skill '{SkillName}'")]
    private static partial void LogScriptExecutionError(ILogger logger, string skillName, string scriptName, Exception exception);
}
