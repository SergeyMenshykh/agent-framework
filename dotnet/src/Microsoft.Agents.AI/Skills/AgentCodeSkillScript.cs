// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Agents.AI;

/// <summary>
/// A code-defined skill script backed by a delegate.
/// </summary>
[Experimental(DiagnosticIds.Experiments.AgentsAIExperiments)]
public sealed class AgentCodeSkillScript : AgentSkillScript
{
    private readonly Func<IDictionary<string, object?>, CancellationToken, Task<string>> _handler;

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentCodeSkillScript"/> class.
    /// </summary>
    /// <param name="name">The script name.</param>
    /// <param name="handler">The delegate that executes the script logic.</param>
    public AgentCodeSkillScript(string name, Func<IDictionary<string, object?>, CancellationToken, Task<string>> handler)
        : base(name)
    {
        this._handler = handler ?? throw new ArgumentNullException(nameof(handler));
    }

    /// <summary>
    /// Executes the script with the given arguments.
    /// </summary>
    /// <param name="arguments">Arguments for script execution.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The script result as a string.</returns>
    public Task<string> ExecuteAsync(IDictionary<string, object?> arguments, CancellationToken cancellationToken = default)
    {
        return this._handler(arguments, cancellationToken);
    }
}
