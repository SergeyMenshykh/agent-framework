// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Agents.AI;

/// <summary>
/// Abstract base class for all agent skills.
/// </summary>
/// <remarks>
/// <para>
/// A skill represents a domain-specific capability with instructions, resources, and scripts.
/// Concrete implementations include <see cref="AgentFileSkill"/> (filesystem-backed),
/// <see cref="AgentCodeSkill"/> (code-defined), and <see cref="AgentClassSkill"/> (class-based).
/// </para>
/// <para>
/// Skill metadata follows the <see href="https://agentskills.io/specification">Agent Skills specification</see>.
/// </para>
/// </remarks>
[Experimental(DiagnosticIds.Experiments.AgentsAIExperiments)]
public abstract class AgentSkill
{
    /// <summary>
    /// Gets the skill name. Lowercase letters, numbers, and hyphens only; no leading, trailing, or consecutive hyphens.
    /// </summary>
    public abstract string Name { get; }

    /// <summary>
    /// Gets the skill description. Used for discovery in the system prompt.
    /// </summary>
    public abstract string Description { get; }

    /// <summary>
    /// Gets the full skill content.
    /// </summary>
    /// <remarks>
    /// For file-based skills this is the raw SKILL.md file content.
    /// For code-defined and class-based skills this is a synthesized XML document
    /// containing name, description, and body (instructions, resources, scripts).
    /// </remarks>
    public abstract string Content { get; }

    /// <summary>
    /// Gets the skill body content.
    /// </summary>
    /// <remarks>
    /// For file-based skills this is the instructions text after the YAML frontmatter.
    /// For code-defined and class-based skills this is an XML document containing
    /// instructions, resources, and scripts elements.
    /// </remarks>
    public abstract string Body { get; }

    /// <summary>
    /// Gets or sets an optional license name or reference.
    /// </summary>
    public string? License { get; set; }

    /// <summary>
    /// Gets or sets optional compatibility information (max 500 chars).
    /// </summary>
    public string? Compatibility { get; set; }

    /// <summary>
    /// Gets or sets optional space-delimited list of pre-approved tools.
    /// </summary>
    public string? AllowedTools { get; set; }

    /// <summary>
    /// Gets or sets the arbitrary key-value metadata for this skill.
    /// </summary>
    public IDictionary<string, string>? Metadata { get; set; }

    /// <summary>
    /// Gets the resources associated with this skill, or <see langword="null"/> if none.
    /// </summary>
    public abstract IReadOnlyList<AgentSkillResource>? Resources { get; }

    /// <summary>
    /// Gets the scripts associated with this skill, or <see langword="null"/> if none.
    /// </summary>
    public abstract IReadOnlyList<AgentSkillScript>? Scripts { get; }
}
