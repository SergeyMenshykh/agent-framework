// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Agents.AI;

/// <summary>
/// A composite skill source that aggregates multiple child sources and routes all operations
/// to the appropriate source that owns each skill.
/// </summary>
[Experimental(DiagnosticIds.Experiments.AgentsAIExperiments)]
public sealed class AgentCompositeSkillsSource : AgentSkillsSource
{
    private readonly IReadOnlyList<AgentSkillsSource> _sources;
    private Dictionary<string, AgentSkillsSource>? _skillOwnership;

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentCompositeSkillsSource"/> class.
    /// </summary>
    /// <param name="sources">The child sources to aggregate.</param>
    public AgentCompositeSkillsSource(IEnumerable<AgentSkillsSource> sources)
    {
        _ = Throw.IfNull(sources);
        this._sources = sources.ToList();
    }

    /// <inheritdoc/>
    public override async Task<IReadOnlyList<AgentSkill>> GetSkillsAsync(CancellationToken cancellationToken = default)
    {
        var allSkills = new List<AgentSkill>();
        var ownership = new Dictionary<string, AgentSkillsSource>(StringComparer.OrdinalIgnoreCase);

        foreach (var source in this._sources)
        {
            IReadOnlyList<AgentSkill> sourceSkills = await source.GetSkillsAsync(cancellationToken).ConfigureAwait(false);
            foreach (var skill in sourceSkills)
            {
                if (!ownership.ContainsKey(skill.Name))
                {
                    ownership[skill.Name] = source;
                    allSkills.Add(skill);
                }
            }
        }

        this._skillOwnership = ownership;

        IReadOnlyList<AgentSkill> result;
        if (this.Filter != null)
        {
            result = allSkills.Where(s => this.Filter(s)).ToList();
        }
        else
        {
            result = allSkills;
        }

        return result;
    }

    /// <inheritdoc/>
    public override Task<string> ReadResourceAsync(AgentSkill skill, string resourceName, CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(skill);
        AgentSkillsSource owner = this.GetOwnerSource(skill);
        return owner.ReadResourceAsync(skill, resourceName, cancellationToken);
    }

    /// <inheritdoc/>
    public override Task<string> ExecuteScriptAsync(AgentSkill skill, AgentSkillScript script, IDictionary<string, object?> arguments, CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(skill);
        AgentSkillsSource owner = this.GetOwnerSource(skill);
        return owner.ExecuteScriptAsync(skill, script, arguments, cancellationToken);
    }

    private AgentSkillsSource GetOwnerSource(AgentSkill skill)
    {
        if (this._skillOwnership == null)
        {
            throw new InvalidOperationException("Skills have not been loaded yet. Call GetSkillsAsync first.");
        }

        if (!this._skillOwnership.TryGetValue(skill.Name, out AgentSkillsSource? owner))
        {
            throw new InvalidOperationException($"Skill '{skill.Name}' is not owned by any source in this composite.");
        }

        return owner;
    }
}
