// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Agents.AI;

/// <summary>
/// A skill script backed by a delegate via an <see cref="AIFunction"/>.
/// </summary>
/// <remarks>
/// <para>
/// Create an instance by passing a regular C# method (as a <see cref="Delegate"/>).
/// The framework uses <see cref="AIFunctionFactory"/> to handle parameter marshaling automatically:
/// </para>
/// <code>
/// new AgentInlineSkillScript(Convert, "convert");
///
/// static string Convert(double value, double factor)
///     =&gt; JsonSerializer.Serialize(new { result = Math.Round(value * factor, 4) });
/// </code>
/// </remarks>
[Experimental(DiagnosticIds.Experiments.AgentsAIExperiments)]
public sealed class AgentInlineSkillScript : AgentSkillScript
{
    private readonly AIFunction _function;

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentInlineSkillScript"/> class from a delegate.
    /// The delegate's parameters and return type are automatically marshaled via <see cref="AIFunctionFactory"/>.
    /// </summary>
    /// <param name="handler">A method to execute when the script is invoked. Parameters are automatically deserialized from JSON.</param>
    /// <param name="name">The script name.</param>
    /// <param name="description">An optional description of the script.</param>
    public AgentInlineSkillScript(Delegate handler, string name, string? description = null)
        : base(Throw.IfNullOrWhitespace(name), description)
    {
        Throw.IfNull(handler);
        this._function = AIFunctionFactory.Create(handler, name: this.Name);
    }

    /// <summary>
    /// Gets the JSON schema describing the parameters accepted by this script, or <see langword="null"/> if not available.
    /// </summary>
    public JsonElement? ParametersSchema => this._function.JsonSchema;

    /// <inheritdoc/>
    public override async Task<object?> RunAsync(AgentSkill skill, AIFunctionArguments arguments, CancellationToken cancellationToken = default)
    {
        return await this._function.InvokeAsync(arguments, cancellationToken).ConfigureAwait(false);
    }
}
