// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Agents.AI;

/// <summary>
/// Represents the YAML frontmatter metadata parsed from a SKILL.md file.
/// </summary>
/// <remarks>
/// <para>
/// Frontmatter is the L1 (discovery) layer of the
/// <see href="https://agentskills.io/specification">Agent Skills specification</see>.
/// It contains the minimal metadata needed to advertise a skill in the system prompt
/// without loading the full skill content.
/// </para>
/// <para>
/// This class is a plain data holder with no validation logic.
/// Use <see cref="AgentSkillFrontmatterValidator"/> to validate frontmatter before use.
/// </para>
/// </remarks>
[Experimental(DiagnosticIds.Experiments.AgentsAIExperiments)]
public sealed class AgentSkillFrontmatter
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AgentSkillFrontmatter"/> class.
    /// </summary>
    /// <param name="name">Skill name in kebab-case.</param>
    /// <param name="description">Skill description for discovery.</param>
    public AgentSkillFrontmatter(string name, string description)
    {
        this.Name = Throw.IfNullOrWhitespace(name);
        this.Description = Throw.IfNullOrWhitespace(description);
    }

    /// <summary>
    /// Gets the skill name. Lowercase letters, numbers, and hyphens only; no leading, trailing, or consecutive hyphens.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the skill description. Used for discovery in the system prompt.
    /// </summary>
    public string Description { get; }

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
}
