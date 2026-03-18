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
/// Use <see cref="AddResource(string, object?, string?)"/>, <see cref="AddResource(string, Delegate, string?)"/>,
/// and <see cref="AddScript(Delegate, string, string?)"/> to register resources and scripts after construction.
/// </remarks>
[Experimental(DiagnosticIds.Experiments.AgentsAIExperiments)]
public sealed class AgentCodeSkill : AgentSkill
{
    private readonly string _instructions;
    private readonly List<AgentSkillResource> _resources = new();
    private readonly List<AgentSkillScript> _scripts = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentCodeSkill"/> class.
    /// </summary>
    /// <param name="name">Skill name.</param>
    /// <param name="description">Skill description.</param>
    /// <param name="instructions">Skill instructions text.</param>
    public AgentCodeSkill(
        string name,
        string description,
        string instructions)
    {
        this.Frontmatter = new AgentSkillFrontmatter(name, description);
        this._instructions = Throw.IfNull(instructions);
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
    /// <param name="name">The resource name.</param>
    /// <param name="value">The static resource value.</param>
    /// <param name="description">An optional description of the resource.</param>
    /// <returns>This instance, for chaining.</returns>
    public AgentCodeSkill AddResource(string name, object? value, string? description = null)
    {
        this._resources.Add(new AgentCodeSkillResource(name, value, description));
        return this;
    }

    /// <summary>
    /// Registers a dynamic resource with this skill, backed by a C# delegate.
    /// The delegate's parameters and return type are automatically marshaled via <c>AIFunctionFactory</c>.
    /// </summary>
    /// <param name="name">The resource name.</param>
    /// <param name="handler">A method that produces the resource value when requested.</param>
    /// <param name="description">An optional description of the resource.</param>
    /// <returns>This instance, for chaining.</returns>
    public AgentCodeSkill AddResource(string name, Delegate handler, string? description = null)
    {
        this._resources.Add(new AgentCodeSkillResource(name, handler, description));
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
    public AgentCodeSkill AddScript(Delegate handler, string name, string? description = null)
    {
        this._scripts.Add(new AgentCodeSkillScript(handler, name, description));
        return this;
    }
}
