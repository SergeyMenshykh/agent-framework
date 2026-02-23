// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.AI;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Agents.AI;

/// <summary>
/// Defines the contract for skill script execution modes.
/// </summary>
/// <remarks>
/// <para>
/// A <see cref="SkillScriptExecutor"/> provides the instructions and tools needed to enable
/// script execution within an agent skill. Concrete implementations determine how scripts
/// are executed (e.g., via the LLM's hosted code interpreter, an external executor, or a hybrid approach).
/// </para>
/// <para>
/// Use the static factory methods to create instances:
/// <list type="bullet">
/// <item><description><see cref="HostedCodeInterpreter"/> — executes scripts using the LLM provider's built-in code interpreter.</description></item>
/// </list>
/// </para>
/// </remarks>
[Experimental(DiagnosticIds.Experiments.AgentsAIExperiments)]
public abstract class SkillScriptExecutor
{
    /// <summary>
    /// Creates a <see cref="SkillScriptExecutor"/> that uses the LLM provider's hosted code interpreter for script execution.
    /// </summary>
    /// <returns>A <see cref="SkillScriptExecutor"/> instance configured for hosted code interpreter execution.</returns>
    public static SkillScriptExecutor HostedCodeInterpreter() => new HostedCodeInterpreterExecutor();

    /// <summary>
    /// Gets the additional instructions to provide to the agent for script execution.
    /// </summary>
    /// <returns>Instructions string, or <see langword="null"/> if no additional instructions are needed.</returns>
    public abstract string? GetInstructions();

    /// <summary>
    /// Gets the additional tools to provide to the agent for script execution.
    /// </summary>
    /// <returns>A read-only list of tools, or <see langword="null"/> if no additional tools are needed.</returns>
    public abstract IReadOnlyList<AITool>? GetTools();
}
