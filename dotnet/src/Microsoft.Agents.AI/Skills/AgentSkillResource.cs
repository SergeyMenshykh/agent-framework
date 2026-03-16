// Copyright (c) Microsoft. All rights reserved.

using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Agents.AI;

/// <summary>
/// Abstract base class for skill resources. A resource provides supplementary content (references, assets) to a skill.
/// </summary>
[Experimental(DiagnosticIds.Experiments.AgentsAIExperiments)]
public abstract class AgentSkillResource
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AgentSkillResource"/> class.
    /// </summary>
    /// <param name="name">The resource name (e.g., relative path or identifier).</param>
    protected AgentSkillResource(string name)
    {
        this.Name = Throw.IfNullOrWhitespace(name);
    }

    /// <summary>
    /// Gets the resource name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Reads the resource content asynchronously.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The resource content.</returns>
    public abstract Task<object?> ReadAsync(CancellationToken cancellationToken = default);
}
