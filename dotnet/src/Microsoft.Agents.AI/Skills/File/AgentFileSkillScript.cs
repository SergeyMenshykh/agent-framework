// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Agents.AI;

/// <summary>
/// A file-path-backed skill script. Represents a script file on disk that requires an external executor to run.
/// </summary>
[Experimental(DiagnosticIds.Experiments.AgentsAIExperiments)]
public sealed class AgentFileSkillScript : AgentSkillScript
{
    private readonly AgentFileSkillScriptExecutor? _executor;

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentFileSkillScript"/> class.
    /// </summary>
    /// <param name="name">The script name.</param>
    /// <param name="path">The file path to the script, relative to the skill directory.</param>
    /// <param name="executor">Optional external executor for running the script.</param>
    internal AgentFileSkillScript(string name, string path, AgentFileSkillScriptExecutor? executor = null)
        : base(name)
    {
        this.Path = Throw.IfNullOrWhitespace(path);
        this._executor = executor;
    }

    /// <summary>
    /// Gets the file path to the script, relative to the skill directory.
    /// </summary>
    public string Path { get; }

    /// <inheritdoc/>
    /// <exception cref="NotSupportedException">
    /// Thrown when no external executor was provided.
    /// </exception>
    public override async Task<object?> ExecuteAsync(AgentSkill skill, AIFunctionArguments arguments, CancellationToken cancellationToken = default)
    {
        if (this._executor == null)
        {
            throw new NotSupportedException($"File-based script '{this.Name}' at '{this.Path}' requires an external executor and cannot be executed directly.");
        }

        return await this._executor(skill, this, arguments, cancellationToken).ConfigureAwait(false);
    }
}
