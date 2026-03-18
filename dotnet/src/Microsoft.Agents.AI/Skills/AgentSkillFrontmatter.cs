// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
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
/// </remarks>
[Experimental(DiagnosticIds.Experiments.AgentsAIExperiments)]
public sealed class AgentSkillFrontmatter
{
    /// <summary>
    /// Maximum allowed length for the <see cref="Name"/> field.
    /// </summary>
    public const int MaxNameLength = 64;

    /// <summary>
    /// Maximum allowed length for the <see cref="Description"/> field.
    /// </summary>
    public const int MaxDescriptionLength = 1024;

    /// <summary>
    /// Maximum allowed length for the <see cref="Compatibility"/> field.
    /// </summary>
    public const int MaxCompatibilityLength = 500;

    // Validates skill names: lowercase letters, numbers, and hyphens only;
    // must not start or end with a hyphen; must not contain consecutive hyphens.
    private static readonly Regex s_validNameRegex = new("^[a-z0-9]([a-z0-9]*-[a-z0-9])*[a-z0-9]*$", RegexOptions.Compiled);

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentSkillFrontmatter"/> class.
    /// </summary>
    /// <param name="name">Skill name in kebab-case.</param>
    /// <param name="description">Skill description for discovery.</param>
    public AgentSkillFrontmatter(string name, string description)
    {
        this.Name = ValidateName(Throw.IfNullOrWhitespace(name));
        this.Description = ValidateDescription(Throw.IfNullOrWhitespace(description));
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

    /// <summary>
    /// Validates and returns the name, or throws if invalid.
    /// </summary>
    private static string ValidateName(string name)
    {
        if (name.Length > MaxNameLength)
        {
            throw new ArgumentException($"Skill name must be {MaxNameLength} characters or fewer.", nameof(name));
        }

        if (!s_validNameRegex.IsMatch(name))
        {
            throw new ArgumentException(
                "Skill name must use only lowercase letters, numbers, and hyphens, and must not start or end with a hyphen or contain consecutive hyphens.",
                nameof(name));
        }

        return name;
    }

    /// <summary>
    /// Validates and returns the description, or throws if invalid.
    /// </summary>
    private static string ValidateDescription(string description)
    {
        if (description.Length > MaxDescriptionLength)
        {
            throw new ArgumentException($"Skill description must be {MaxDescriptionLength} characters or fewer.", nameof(description));
        }

        return description;
    }
}
