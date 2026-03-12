// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Agents.AI;

/// <summary>
/// A skill source that holds class-based <see cref="AgentClassSkill"/> instances.
/// </summary>
[Experimental(DiagnosticIds.Experiments.AgentsAIExperiments)]
public sealed class AgentClassSkillsSource : AgentSkillsSource
{
    private readonly IReadOnlyList<AgentClassSkill> _skills;

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentClassSkillsSource"/> class.
    /// </summary>
    /// <param name="skills">The class-based skills to include in this source.</param>
    public AgentClassSkillsSource(IEnumerable<AgentClassSkill> skills)
    {
        _ = Throw.IfNull(skills);
        this._skills = skills.ToList();
    }

    /// <inheritdoc/>
    public override Task<IReadOnlyList<AgentSkill>> GetSkillsAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<AgentSkill> result = this._skills.ToList<AgentSkill>();
        return Task.FromResult(result);
    }
}
