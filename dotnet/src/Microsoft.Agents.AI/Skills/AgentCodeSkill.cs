// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Agents.AI;

/// <summary>
/// A skill defined entirely in code with static/dynamic resources and delegate-backed scripts.
/// </summary>
/// <remarks>
/// Resources and scripts are provided at construction time and are immutable after construction.
/// </remarks>
[Experimental(DiagnosticIds.Experiments.AgentsAIExperiments)]
public sealed class AgentCodeSkill : AgentSkill
{
    private readonly IReadOnlyList<AgentSkillResource> _resources;
    private readonly IReadOnlyList<AgentSkillScript> _scripts;

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentCodeSkill"/> class.
    /// </summary>
    /// <param name="name">Skill name.</param>
    /// <param name="description">Skill description.</param>
    /// <param name="body">Skill instructions body.</param>
    /// <param name="resources">Optional resources for this skill.</param>
    /// <param name="scripts">Optional scripts for this skill.</param>
    public AgentCodeSkill(
        string name,
        string description,
        string body,
        IReadOnlyList<AgentSkillResource>? resources = null,
        IReadOnlyList<AgentSkillScript>? scripts = null)
    {
        this.Name = Throw.IfNullOrWhitespace(name);
        this.Description = Throw.IfNullOrWhitespace(description);
        this.Body = Throw.IfNull(body);
        this._resources = resources ?? Array.Empty<AgentSkillResource>();
        this._scripts = scripts ?? Array.Empty<AgentSkillScript>();
    }

    /// <inheritdoc/>
    public override string Name { get; }

    /// <inheritdoc/>
    public override string Description { get; }

    /// <inheritdoc/>
    public override string Body { get; }

    /// <inheritdoc/>
    public override IReadOnlyList<AgentSkillResource> Resources => this._resources;

    /// <inheritdoc/>
    public override IReadOnlyList<AgentSkillScript> Scripts => this._scripts;
}
