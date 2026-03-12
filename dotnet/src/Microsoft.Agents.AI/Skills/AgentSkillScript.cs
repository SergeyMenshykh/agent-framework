// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Agents.AI;

/// <summary>
/// Abstract base class for skill scripts. A script represents an executable action associated with a skill.
/// </summary>
[Experimental(DiagnosticIds.Experiments.AgentsAIExperiments)]
public abstract class AgentSkillScript
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AgentSkillScript"/> class.
    /// </summary>
    /// <param name="name">The script name.</param>
    protected AgentSkillScript(string name)
    {
        this.Name = Throw.IfNullOrWhitespace(name);
    }

    /// <summary>
    /// Gets the script name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Executes the script with the given arguments.
    /// </summary>
    /// <param name="arguments">Arguments for script execution.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The script execution result as a string.</returns>
    public abstract Task<string> ExecuteAsync(IDictionary<string, object?> arguments, CancellationToken cancellationToken = default);
}
