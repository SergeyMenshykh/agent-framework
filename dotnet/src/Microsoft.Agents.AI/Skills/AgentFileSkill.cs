// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Agents.AI;

/// <summary>
/// An <see cref="AgentSkill"/> discovered from a filesystem directory backed by a SKILL.md file.
/// </summary>
[Experimental(DiagnosticIds.Experiments.AgentsAIExperiments)]
public sealed class AgentFileSkill : AgentSkill
{
    private readonly IReadOnlyList<AgentSkillResource> _resources;
    private readonly IReadOnlyList<AgentSkillScript> _scripts;

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentFileSkill"/> class.
    /// </summary>
    /// <param name="name">Skill name.</param>
    /// <param name="description">Skill description.</param>
    /// <param name="body">The SKILL.md content after the closing frontmatter delimiter.</param>
    /// <param name="sourcePath">Absolute path to the directory containing this skill.</param>
    /// <param name="resources">Resources discovered for this skill.</param>
    /// <param name="scripts">Scripts discovered for this skill.</param>
    public AgentFileSkill(
        string name,
        string description,
        string body,
        string sourcePath,
        IReadOnlyList<AgentSkillResource>? resources = null,
        IReadOnlyList<AgentSkillScript>? scripts = null)
    {
        this.Name = Throw.IfNullOrWhitespace(name);
        this.Description = Throw.IfNullOrWhitespace(description);
        this.Body = Throw.IfNull(body);
        this.SourcePath = Throw.IfNullOrWhitespace(sourcePath);
        this._resources = resources ?? System.Array.Empty<AgentSkillResource>();
        this._scripts = scripts ?? System.Array.Empty<AgentSkillScript>();
    }

    /// <inheritdoc/>
    public override string Name { get; }

    /// <inheritdoc/>
    public override string Description { get; }

    /// <inheritdoc/>
    public override string Body { get; }

    /// <summary>
    /// Gets the directory path where the skill was discovered.
    /// </summary>
    public string SourcePath { get; }

    /// <inheritdoc/>
    public override IReadOnlyList<AgentSkillResource> Resources => this._resources;

    /// <inheritdoc/>
    public override IReadOnlyList<AgentSkillScript> Scripts => this._scripts;
}
