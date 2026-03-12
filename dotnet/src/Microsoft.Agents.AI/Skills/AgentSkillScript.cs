// Copyright (c) Microsoft. All rights reserved.

using System.Diagnostics.CodeAnalysis;
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
}
