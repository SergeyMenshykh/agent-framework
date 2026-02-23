// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using Microsoft.Extensions.AI;

namespace Microsoft.Agents.AI;

/// <summary>
/// A <see cref="SkillScriptExecutor"/> that uses the LLM provider's hosted code interpreter for script execution.
/// </summary>
/// <remarks>
/// This executor directs the LLM to load scripts via <c>read_skill_resource</c> and execute them
/// using the provider's built-in code interpreter. A <see cref="HostedCodeInterpreterTool"/> is
/// registered to signal the provider to enable its code interpreter sandbox.
/// </remarks>
internal sealed class HostedCodeInterpreterExecutor : SkillScriptExecutor
{
    // Leading and trailing blank lines are intentional to logically separate this content
    // from the surrounding text when merged into the FileAgentSkillsProvider instructions template.
    private const string Instructions =
        """

        Some skills include executable scripts (e.g., Python files) in their resources.
        When a skill's instructions reference a script:
        1. Use `read_skill_resource` to load the script content
        2. Execute the script using the code interpreter

        """;

    private static readonly AITool[] s_tools = [new HostedCodeInterpreterTool()];

    /// <inheritdoc />
    public override string GetInstructions() => Instructions;

    /// <inheritdoc />
    public override IReadOnlyList<AITool> GetTools() => s_tools;
}
