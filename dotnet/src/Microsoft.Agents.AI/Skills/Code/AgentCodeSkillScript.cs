// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;

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
/// new AgentCodeSkillScript(Convert, "convert");
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
    /// <param name="name">The script name.</param>
    /// <param name="description">An optional description of the script.</param>
    public AgentCodeSkillScript(Delegate handler, string name, string? description = null)
        : base(Throw.IfNullOrWhitespace(name), description)
    {
        Throw.IfNull(handler);
        this._function = AIFunctionFactory.Create(handler, name: this.Name);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentCodeSkillScript"/> class with a raw handler delegate.
    /// </summary>
    /// <param name="name">The script name.</param>
    /// <param name="handler">The delegate that executes the script logic.</param>
    /// <param name="description">An optional description of the script.</param>
    /// <remarks>
    /// Prefer the <see cref="AgentCodeSkillScript(Delegate, string, string?)"/> constructor which provides
    /// automatic parameter marshaling. This constructor requires manual argument handling.
    /// </remarks>
    public AgentCodeSkillScript(string name, Func<IDictionary<string, object?>, CancellationToken, Task<string>> handler, string? description = null)
        : base(name, description)
    {
        if (handler == null)
        {
            throw new ArgumentNullException(nameof(handler));
        }

        this._function = AIFunctionFactory.Create(
            (AIFunctionArguments arguments, CancellationToken cancellationToken) => handler(arguments, cancellationToken),
            name: name);
    }

    /// <summary>
    /// Gets the JSON schema describing the parameters accepted by this script, or <see langword="null"/> if not available.
    /// </summary>
    public JsonElement? ParametersSchema => this._function.JsonSchema;

    /// <inheritdoc/>
    public override async Task<object?> ExecuteAsync(AgentSkill skill, AIFunctionArguments arguments, CancellationToken cancellationToken = default)
    {
        return await this._function.InvokeAsync(arguments, cancellationToken).ConfigureAwait(false);
    }
}
