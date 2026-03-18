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
/// A composite skill source that aggregates multiple child sources.
/// </summary>
[Experimental(DiagnosticIds.Experiments.AgentsAIExperiments)]
public sealed class CompositeAgentSkillsSource : AgentSkillsSource
{
    private readonly IReadOnlyList<AgentSkillsSource> _sources;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompositeAgentSkillsSource"/> class.
    /// </summary>
    /// <param name="sources">The child sources to aggregate.</param>
    public CompositeAgentSkillsSource(IEnumerable<AgentSkillsSource> sources)
    {
        _ = Throw.IfNull(sources);
        this._sources = sources.ToList();
    }

    /// <inheritdoc/>
    public override async Task<IReadOnlyList<AgentSkill>> GetSkillsAsync(CancellationToken cancellationToken = default)
    {
        var allSkills = new List<AgentSkill>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var source in this._sources)
        {
            IReadOnlyList<AgentSkill> sourceSkills = await source.GetSkillsAsync(cancellationToken).ConfigureAwait(false);
            foreach (var skill in sourceSkills)
            {
                if (seen.Add(skill.Frontmatter.Name))
                {
                    allSkills.Add(skill);
                }
            }
        }

        return allSkills;
    }
}
