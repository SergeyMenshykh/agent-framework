// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Agents.AI;

/// <summary>
/// A skill source that discovers skills from filesystem directories containing SKILL.md files.
/// </summary>
/// <remarks>
/// Wraps the existing <see cref="FileAgentSkillLoader"/> to discover, parse, and validate skills
/// from disk. Resource reads are guarded against path traversal and symlink escape.
/// </remarks>
[Experimental(DiagnosticIds.Experiments.AgentsAIExperiments)]
public sealed class AgentFileSkillsSource : AgentSkillsSource
{
    private readonly IReadOnlyList<AgentFileSkill> _skills;
    private readonly Dictionary<string, AgentFileSkill> _skillsByName;
    private readonly Dictionary<string, FileAgentSkill> _internalSkills;
    private readonly FileAgentSkillLoader _loader;

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentFileSkillsSource"/> class.
    /// </summary>
    /// <param name="skillPath">Path to search for skills.</param>
    /// <param name="allowedResourceExtensions">Optional resource extension filter.</param>
    /// <param name="loggerFactory">Optional logger factory.</param>
    public AgentFileSkillsSource(string skillPath, IEnumerable<string>? allowedResourceExtensions = null, ILoggerFactory? loggerFactory = null)
        : this(new[] { skillPath }, allowedResourceExtensions, loggerFactory)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentFileSkillsSource"/> class.
    /// </summary>
    /// <param name="skillPaths">Paths to search for skills.</param>
    /// <param name="allowedResourceExtensions">Optional resource extension filter.</param>
    /// <param name="loggerFactory">Optional logger factory.</param>
    public AgentFileSkillsSource(IEnumerable<string> skillPaths, IEnumerable<string>? allowedResourceExtensions = null, ILoggerFactory? loggerFactory = null)
    {
        _ = Throw.IfNull(skillPaths);

        var logger = (loggerFactory ?? NullLoggerFactory.Instance).CreateLogger<AgentFileSkillsSource>();
        this._loader = new FileAgentSkillLoader(logger, allowedResourceExtensions);

        this._internalSkills = this._loader.DiscoverAndLoadSkills(skillPaths);

        var skills = new List<AgentFileSkill>(this._internalSkills.Count);
        this._skillsByName = new Dictionary<string, AgentFileSkill>(StringComparer.OrdinalIgnoreCase);

        foreach (var kvp in this._internalSkills)
        {
            var resources = new List<AgentSkillResource>(kvp.Value.ResourceNames.Count);
            foreach (string resourceName in kvp.Value.ResourceNames)
            {
                string fullPath = System.IO.Path.Combine(kvp.Value.SourcePath, resourceName.Replace('/', System.IO.Path.DirectorySeparatorChar));
                resources.Add(new AgentFileSkillResource(resourceName, fullPath));
            }

            var agentSkill = new AgentFileSkill(
                kvp.Value.Frontmatter.Name,
                kvp.Value.Frontmatter.Description,
                kvp.Value.Body,
                kvp.Value.SourcePath,
                resources);

            skills.Add(agentSkill);
            this._skillsByName[agentSkill.Name] = agentSkill;
        }

        this._skills = skills;
    }

    /// <inheritdoc/>
    public override Task<IReadOnlyList<AgentSkill>> GetSkillsAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<AgentSkill> result;
        if (this.Filter != null)
        {
            result = this._skills.Where(s => this.Filter(s)).ToList();
        }
        else
        {
            result = this._skills.ToList<AgentSkill>();
        }

        return Task.FromResult(result);
    }

    /// <inheritdoc/>
    public override async Task<string> ReadResourceAsync(AgentSkill skill, string resourceName, CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(skill);
        _ = Throw.IfNullOrWhitespace(resourceName);

        if (!(skill is AgentFileSkill) || !this._skillsByName.ContainsKey(skill.Name))
        {
            throw new InvalidOperationException($"Skill '{skill.Name}' is not owned by this source.");
        }

        if (!this._internalSkills.TryGetValue(skill.Name, out FileAgentSkill? internalSkill))
        {
            throw new InvalidOperationException($"Skill '{skill.Name}' not found in internal cache.");
        }

        return await this._loader.ReadSkillResourceAsync(internalSkill, resourceName, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public override Task<string> ExecuteScriptAsync(AgentSkill skill, AgentSkillScript script, IDictionary<string, object?> arguments, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("File-based skill script execution requires an external executor. Configure a script executor on the skill source or provider.");
    }
}
