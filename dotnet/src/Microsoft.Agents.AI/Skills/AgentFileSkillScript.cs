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
/// A file-path-backed skill script. Represents a script file on disk that requires an external executor to run.
/// </summary>
[Experimental(DiagnosticIds.Experiments.AgentsAIExperiments)]
public sealed class AgentFileSkillScript : AgentSkillScript
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AgentFileSkillScript"/> class.
    /// </summary>
    /// <param name="name">The script name.</param>
    /// <param name="fullPath">The absolute file path to the script.</param>
    public AgentFileSkillScript(string name, string fullPath)
        : base(name)
    {
        this.FullPath = Throw.IfNullOrWhitespace(fullPath);
    }

    /// <summary>
    /// Gets the absolute file path to the script.
    /// </summary>
    public string FullPath { get; }

    /// <inheritdoc/>
    /// <exception cref="NotSupportedException">
    /// File-based scripts require an external executor and cannot be executed directly.
    /// </exception>
    public override Task<string> ExecuteAsync(IDictionary<string, object?> arguments, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException($"File-based script '{this.Name}' at '{this.FullPath}' requires an external executor and cannot be executed directly.");
    }
}
