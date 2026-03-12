// Copyright (c) Microsoft. All rights reserved.

using System.Diagnostics.CodeAnalysis;
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
}
