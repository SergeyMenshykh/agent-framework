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
/// A skill source that holds class-based <see cref="AgentClassSkill"/> instances.
/// </summary>
[Experimental(DiagnosticIds.Experiments.AgentsAIExperiments)]
public sealed class AgentClassSkillsSource : AgentSkillsSource
{
    private readonly IReadOnlyList<AgentClassSkill> _skills;
    private readonly Dictionary<string, AgentClassSkill> _skillsByName;

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentClassSkillsSource"/> class.
    /// </summary>
    /// <param name="skills">The class-based skills to include in this source.</param>
    public AgentClassSkillsSource(IEnumerable<AgentClassSkill> skills)
    {
        _ = Throw.IfNull(skills);

        var list = new List<AgentClassSkill>();
        this._skillsByName = new Dictionary<string, AgentClassSkill>(StringComparer.OrdinalIgnoreCase);

        foreach (var skill in skills)
        {
            list.Add(skill);
            this._skillsByName[skill.Name] = skill;
        }

        this._skills = list;
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
            throw new InvalidOperationException($"Resource '{resourceName}' not found in skill '{skill.Name}'.");
        }

        return await resource.ReadAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public override async Task<string> ExecuteScriptAsync(AgentSkill skill, AgentSkillScript script, IDictionary<string, object?> arguments, CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(skill);
        _ = Throw.IfNull(script);

        if (!(script is AgentCodeSkillScript codeScript))
        {
            throw new InvalidOperationException($"Script '{script.Name}' is not a code-defined script. Class-based skills should use AgentCodeSkillScript.");
        }

        return await codeScript.ExecuteAsync(arguments ?? new Dictionary<string, object?>(), cancellationToken).ConfigureAwait(false);
    }
}
