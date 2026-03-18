// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Agents.AI;

/// <summary>
/// A code-defined skill resource. Supports both static values and dynamic (computed) values.
/// </summary>
[Experimental(DiagnosticIds.Experiments.AgentsAIExperiments)]
public sealed class AgentCodeSkillResource : AgentSkillResource
{
    private readonly AIFunction? _function;
    private readonly object? _staticValue;

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentCodeSkillResource"/> class with a static value.
    /// </summary>
    /// <param name="name">The resource name.</param>
    /// <param name="value">The static resource value.</param>
    /// <param name="description">An optional description of the resource.</param>
    public AgentCodeSkillResource(string name, object? value, string? description = null)
        : base(name, description)
    {
        this._staticValue = value;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentCodeSkillResource"/> class with a dynamic value.
    /// </summary>
    /// <param name="name">The resource name.</param>
    /// <param name="handler"></param>
    /// <param name="description">An optional description of the resource.</param>
    public AgentCodeSkillResource(string name, Delegate handler, string? description = null)
        : base(name, description)
    {
        this._function = AIFunctionFactory.Create(handler, name: this.Name);
    }

    /// <inheritdoc/>
    public override async Task<object?> ReadAsync(AIFunctionArguments arguments, CancellationToken cancellationToken = default)
    {
        if (this._function is not null)
        {
            return await this._function.InvokeAsync(arguments, cancellationToken).ConfigureAwait(false);
        }

        return this._staticValue;
    }
}
