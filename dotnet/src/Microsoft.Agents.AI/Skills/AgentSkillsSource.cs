// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Agents.AI;

/// <summary>
/// Abstract base class for skill sources. A skill source provides skills from a specific origin
/// (filesystem, remote server, database, in-memory, etc.).
/// </summary>
/// <remarks>
/// <para>
/// Each source owns a set of skills and is responsible for:
/// </para>
/// <list type="bullet">
/// <item><description>Listing available skills.</description></item>
/// <item><description>Loading skill content.</description></item>
/// <item><description>Reading skill resources.</description></item>
/// <item><description>Executing skill scripts.</description></item>
/// </list>
/// </remarks>
[Experimental(DiagnosticIds.Experiments.AgentsAIExperiments)]
public abstract class AgentSkillsSource
{
    /// <summary>
    /// Gets or sets an optional filter applied to skills from this source before they are surfaced to the provider.
    /// </summary>
    public Func<AgentSkill, bool>? Filter { get; set; }

    /// <summary>
    /// Gets the skills provided by this source.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of skills from this source.</returns>
    public abstract Task<IReadOnlyList<AgentSkill>> GetSkillsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads a resource from a skill owned by this source.
    /// </summary>
    /// <param name="skill">The skill that owns the resource.</param>
    /// <param name="resourceName">The resource name to read.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The resource content as a string.</returns>
    public abstract Task<string> ReadResourceAsync(AgentSkill skill, string resourceName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a script from a skill owned by this source.
    /// </summary>
    /// <param name="skill">The skill that owns the script.</param>
    /// <param name="script">The script to execute.</param>
    /// <param name="arguments">Arguments for script execution.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The script execution result as a string.</returns>
    public abstract Task<string> ExecuteScriptAsync(AgentSkill skill, AgentSkillScript script, IDictionary<string, object?> arguments, CancellationToken cancellationToken = default);
}
