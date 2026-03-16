// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Agents.AI;

/// <summary>
/// A code-defined skill script backed by an <see cref="AIFunction"/>.
/// </summary>
/// <remarks>
/// <para>
/// Create an instance by passing a regular C# method (as a <see cref="Delegate"/>).
/// The framework uses <see cref="AIFunctionFactory"/> to handle parameter marshaling automatically:
/// </para>
/// <code>
/// new AgentCodeSkillScript(Convert);
///
/// static string Convert(double value, double factor)
///     =&gt; JsonSerializer.Serialize(new { result = Math.Round(value * factor, 4) });
/// </code>
/// </remarks>
[Experimental(DiagnosticIds.Experiments.AgentsAIExperiments)]
public sealed class AgentCodeSkillScript : AgentSkillScript
{
    private readonly AIFunction _function;

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentCodeSkillScript"/> class from a delegate.
    /// The delegate's parameters and return type are automatically marshaled via <see cref="AIFunctionFactory"/>.
    /// </summary>
    /// <param name="handler">A method to execute when the script is invoked. Parameters are automatically deserialized from JSON.</param>
    /// <param name="name">Optional script name. Defaults to the method name.</param>
    public AgentCodeSkillScript(Delegate handler, string? name = null)
        : base(name ?? (handler ?? throw new ArgumentNullException(nameof(handler))).Method.Name)
    {
        this._function = AIFunctionFactory.Create(handler, name: this.Name);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentCodeSkillScript"/> class with a raw handler delegate.
    /// </summary>
    /// <param name="name">The script name.</param>
    /// <param name="handler">The delegate that executes the script logic.</param>
    /// <remarks>
    /// Prefer the <see cref="AgentCodeSkillScript(Delegate, string?)"/> constructor which provides
    /// automatic parameter marshaling. This constructor requires manual argument handling.
    /// </remarks>
    public AgentCodeSkillScript(string name, Func<IDictionary<string, object?>, CancellationToken, Task<string>> handler)
        : base(name)
    {
        if (handler == null)
        {
            throw new ArgumentNullException(nameof(handler));
        }

        this._function = AIFunctionFactory.Create(
            (AIFunctionArguments arguments, CancellationToken cancellationToken) => handler(arguments, cancellationToken),
            name: name);
    }

    /// <inheritdoc/>
    public override async Task<object?> ExecuteAsync(AgentSkill skill, AIFunctionArguments arguments, CancellationToken cancellationToken = default)
    {
        return await this._function.InvokeAsync(arguments, cancellationToken).ConfigureAwait(false);
    }
}
