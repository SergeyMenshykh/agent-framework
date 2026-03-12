// Copyright (c) Microsoft. All rights reserved.

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
[Experimental(DiagnosticIds.Experiments.AgentsAIExperiments)]
public sealed class FileAgentSkillsSource : AgentSkillsSource
{
    private readonly IReadOnlyList<AgentFileSkill> _skills;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileAgentSkillsSource"/> class.
    /// </summary>
    /// <param name="skillPath">Path to search for skills.</param>
    /// <param name="allowedResourceExtensions">Optional resource extension filter.</param>
    /// <param name="loggerFactory">Optional logger factory.</param>
    public FileAgentSkillsSource(string skillPath, IEnumerable<string>? allowedResourceExtensions = null, ILoggerFactory? loggerFactory = null)
        : this(new[] { skillPath }, allowedResourceExtensions, loggerFactory)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FileAgentSkillsSource"/> class.
    /// </summary>
    /// <param name="skillPaths">Paths to search for skills.</param>
    /// <param name="allowedResourceExtensions">Optional resource extension filter.</param>
    /// <param name="loggerFactory">Optional logger factory.</param>
    public FileAgentSkillsSource(IEnumerable<string> skillPaths, IEnumerable<string>? allowedResourceExtensions = null, ILoggerFactory? loggerFactory = null)
    {
        _ = Throw.IfNull(skillPaths);

        var logger = (loggerFactory ?? NullLoggerFactory.Instance).CreateLogger<FileAgentSkillsSource>();
        var loader = new FileAgentSkillLoader(logger, allowedResourceExtensions);

        var internalSkills = loader.DiscoverAndLoadSkills(skillPaths);

        var skills = new List<AgentFileSkill>(internalSkills.Count);

        foreach (var kvp in internalSkills)
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
        }

        this._skills = skills;
    }

    /// <inheritdoc/>
    public override Task<IReadOnlyList<AgentSkill>> GetSkillsAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<AgentSkill> result = this._skills.ToList<AgentSkill>();
        return Task.FromResult(result);
    }
}
