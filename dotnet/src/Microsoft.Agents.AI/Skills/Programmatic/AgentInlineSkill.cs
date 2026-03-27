// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.AI;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Agents.AI;

/// <summary>
/// A skill defined entirely in code with static/dynamic resources and delegate-backed scripts.
/// </summary>
/// <remarks>
/// Use <see cref="AddResource(object, string, string?)"/>, <see cref="AddResource(Delegate, string, string?)"/>,
/// and <see cref="AddScript(Delegate, string, string?)"/> to register resources and scripts after construction.
/// </remarks>
[Experimental(DiagnosticIds.Experiments.AgentsAIExperiments)]
public sealed class AgentInlineSkill : AgentSkill
{
    private readonly string _instructions;
    private readonly List<AgentSkillResource> _resources = new();
    private readonly List<AgentSkillScript> _scripts = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentInlineSkill"/> class
    /// with a pre-built <see cref="AgentSkillFrontmatter"/>.
    /// </summary>
    /// <param name="frontmatter">The skill frontmatter containing name, description, and other metadata.</param>
    /// <param name="instructions">Skill instructions text.</param>
    public AgentInlineSkill(
        AgentSkillFrontmatter frontmatter,
        string instructions)
    {
        this.Frontmatter = Throw.IfNull(frontmatter);
        this._instructions = Throw.IfNull(instructions);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentInlineSkill"/> class
    /// with all frontmatter properties specified individually.
    /// </summary>
    /// <param name="name">Skill name in kebab-case.</param>
    /// <param name="description">Skill description for discovery.</param>
    /// <param name="instructions">Skill instructions text.</param>
    /// <param name="license">Optional license name or reference.</param>
    /// <param name="compatibility">Optional compatibility information (max 500 chars).</param>
    /// <param name="allowedTools">Optional space-delimited list of pre-approved tools.</param>
    /// <param name="metadata">Optional arbitrary key-value metadata.</param>
    public AgentInlineSkill(
        string name,
        string description,
        string instructions,
        string? license = null,
        string? compatibility = null,
        string? allowedTools = null,
        AdditionalPropertiesDictionary? metadata = null)
        : this(
            new AgentSkillFrontmatter(name, description, compatibility)
            {
                License = license,
                AllowedTools = allowedTools,
                Metadata = metadata,
            },
            instructions)
    {
    }

    /// <inheritdoc/>
    public override AgentSkillFrontmatter Frontmatter { get; }

    /// <inheritdoc/>
    public override string Content => SkillContentBuilder.BuildContent(this.Frontmatter.Name, this.Frontmatter.Description, SkillContentBuilder.BuildBody(this._instructions, this.Resources, this.Scripts));

    /// <inheritdoc/>
    public override IReadOnlyList<AgentSkillResource>? Resources => this._resources.Count > 0 ? this._resources : null;

    /// <inheritdoc/>
    public override IReadOnlyList<AgentSkillScript>? Scripts => this._scripts.Count > 0 ? this._scripts : null;

    /// <summary>
    /// Registers a static resource with this skill.
    /// </summary>
    /// <param name="value">The static resource value.</param>
    /// <param name="name">The resource name.</param>
    /// <param name="description">An optional description of the resource.</param>
    /// <returns>This instance, for chaining.</returns>
    public AgentInlineSkill AddResource(object value, string name, string? description = null)
    {
        this._resources.Add(new AgentInlineSkillResource(value, name, description));
        return this;
    }

    /// <summary>
    /// Registers a dynamic resource with this skill, backed by a C# delegate.
    /// The delegate's parameters and return type are automatically marshaled via <c>AIFunctionFactory</c>.
    /// </summary>
    /// <param name="handler">A method that produces the resource value when requested.</param>
    /// <param name="name">The resource name.</param>
    /// <param name="description">An optional description of the resource.</param>
    /// <returns>This instance, for chaining.</returns>
    public AgentInlineSkill AddResource(Delegate handler, string name, string? description = null)
    {
        this._resources.Add(new AgentInlineSkillResource(handler, name, description));
        return this;
    }

    /// <summary>
    /// Registers a script with this skill, backed by a C# delegate.
    /// The delegate's parameters and return type are automatically marshaled via <c>AIFunctionFactory</c>.
    /// </summary>
    /// <param name="handler">A method to execute when the script is invoked.</param>
    /// <param name="name">The script name.</param>
    /// <param name="description">An optional description of the script.</param>
    /// <returns>This instance, for chaining.</returns>
    public AgentInlineSkill AddScript(Delegate handler, string name, string? description = null)
    {
        this._scripts.Add(new AgentInlineSkillScript(handler, name, description));
        return this;
    }
}
