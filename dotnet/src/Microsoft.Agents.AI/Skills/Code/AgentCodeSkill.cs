// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Agents.AI;

/// <summary>
/// A skill defined entirely in code with static/dynamic resources and delegate-backed scripts.
/// </summary>
/// <remarks>
/// Use <see cref="AddResource(string, object?)"/>, <see cref="AddResource(string, Func{CancellationToken, Task{object}})"/>,
/// and <see cref="AddScript(Delegate, string)"/> to register resources and scripts after construction.
/// </remarks>
[Experimental(DiagnosticIds.Experiments.AgentsAIExperiments)]
public sealed class AgentCodeSkill : AgentSkill
{
    private readonly List<AgentSkillResource> _resources = new();
    private readonly List<AgentSkillScript> _scripts = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentCodeSkill"/> class.
    /// </summary>
    /// <param name="name">Skill name.</param>
    /// <param name="description">Skill description.</param>
    /// <param name="body">Skill instructions body.</param>
    public AgentCodeSkill(
        string name,
        string description,
        string body)
    {
        this.Name = Throw.IfNullOrWhitespace(name);
        this.Description = Throw.IfNullOrWhitespace(description);
        this.Content = $"---\nname: {name}\ndescription: {description}\n---\n{Throw.IfNull(body)}";
        this.Body = Throw.IfNull(body);
    }

    /// <inheritdoc/>
    public override string Name { get; }

    /// <inheritdoc/>
    public override string Description { get; }

    /// <inheritdoc/>
    public override string Content { get; }

    /// <inheritdoc/>
    public override string Body { get; }

    /// <inheritdoc/>
    public override IReadOnlyList<AgentSkillResource>? Resources => this._resources.Count > 0 ? this._resources : null;

    /// <inheritdoc/>
    public override IReadOnlyList<AgentSkillScript>? Scripts => this._scripts.Count > 0 ? this._scripts : null;

    /// <summary>
    /// Registers a static resource with this skill.
    /// </summary>
    /// <param name="name">The resource name.</param>
    /// <param name="value">The static resource value.</param>
    /// <returns>This instance, for chaining.</returns>
    public AgentCodeSkill AddResource(string name, object? value)
    {
        this._resources.Add(new AgentCodeSkillResource(name, value));
        return this;
    }

    /// <summary>
    /// Registers a dynamic resource with this skill, computed at runtime via a factory delegate.
    /// </summary>
    /// <param name="name">The resource name.</param>
    /// <param name="valueFactory">A function that produces the resource value when requested.</param>
    /// <returns>This instance, for chaining.</returns>
    public AgentCodeSkill AddResource(string name, Func<CancellationToken, Task<object?>> valueFactory)
    {
        this._resources.Add(new AgentCodeSkillResource(name, valueFactory));
        return this;
    }

    /// <summary>
    /// Registers a script with this skill, backed by a C# delegate.
    /// The delegate's parameters and return type are automatically marshaled via <c>AIFunctionFactory</c>.
    /// </summary>
    /// <param name="handler">A method to execute when the script is invoked.</param>
    /// <param name="name">The script name.</param>
    /// <returns>This instance, for chaining.</returns>
    public AgentCodeSkill AddScript(Delegate handler, string name)
    {
        this._scripts.Add(new AgentCodeSkillScript(handler, name));
        return this;
    }
}
