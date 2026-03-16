// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Agents.AI;

/// <summary>
/// A code-defined skill resource. Supports both static values and dynamic (computed) values.
/// </summary>
[Experimental(DiagnosticIds.Experiments.AgentsAIExperiments)]
public sealed class AgentCodeSkillResource : AgentSkillResource
{
    private readonly Func<CancellationToken, Task<object?>>? _dynamicValue;
    private readonly object? _staticValue;
    private readonly bool _isDynamic;

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentCodeSkillResource"/> class with a static value.
    /// </summary>
    /// <param name="name">The resource name.</param>
    /// <param name="value">The static resource value.</param>
    public AgentCodeSkillResource(string name, object? value)
        : base(name)
    {
        this._staticValue = value;
        this._isDynamic = false;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentCodeSkillResource"/> class with a dynamic value.
    /// </summary>
    /// <param name="name">The resource name.</param>
    /// <param name="valueFactory">A function that produces the resource value when requested.</param>
    public AgentCodeSkillResource(string name, Func<CancellationToken, Task<object?>> valueFactory)
        : base(name)
    {
        this._dynamicValue = valueFactory ?? throw new ArgumentNullException(nameof(valueFactory));
        this._isDynamic = true;
    }

    /// <inheritdoc/>
    public override async Task<object?> ReadAsync(CancellationToken cancellationToken = default)
    {
        if (this._isDynamic)
        {
            return await this._dynamicValue!(cancellationToken).ConfigureAwait(false);
        }

        return this._staticValue;
    }
}
