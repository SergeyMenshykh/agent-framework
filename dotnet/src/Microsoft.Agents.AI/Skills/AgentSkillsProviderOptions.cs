// Copyright (c) Microsoft. All rights reserved.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Agents.AI;

/// <summary>
/// Configuration options for <see cref="AgentSkillsProvider"/>.
/// </summary>
[Experimental(DiagnosticIds.Experiments.AgentsAIExperiments)]
public sealed class AgentSkillsProviderOptions
{
    /// <summary>
    /// Gets or sets a custom system prompt template for advertising skills.
    /// Use <c>{0}</c> as the placeholder for the generated skills list.
    /// When <see langword="null"/>, a default template is used.
    /// </summary>
    public string? SkillsInstructionPrompt { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether script execution requires approval.
    /// When <see langword="true"/>, script execution is blocked until approved.
    /// Defaults to <see langword="false"/>.
    /// </summary>
    public bool ScriptApproval { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the <c>load_skill</c> tool should return only the
    /// skill body (with YAML frontmatter stripped) instead of the full skill content.
    /// When <see langword="false"/> (default), the full content including frontmatter is returned.
    /// When <see langword="true"/>, only the body (instructions after the frontmatter) is returned.
    /// </summary>
    public bool OmitFrontmatter { get; set; }
}
