// Copyright (c) Microsoft. All rights reserved.

using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Agents.AI;

/// <summary>
/// Validates <see cref="AgentSkillFrontmatter"/> instances against the
/// <see href="https://agentskills.io/specification">Agent Skills specification</see> rules.
/// </summary>
/// <remarks>
/// Skills with invalid frontmatter should be excluded from the skill set and the reason logged.
/// Use <see cref="Validate(AgentSkillFrontmatter, out string?)"/> to check frontmatter validity before accepting a skill.
/// </remarks>
[Experimental(DiagnosticIds.Experiments.AgentsAIExperiments)]
internal static class AgentSkillFrontmatterValidator
{
    /// <summary>
    /// Maximum allowed length for the skill name.
    /// </summary>
    internal const int MaxNameLength = 64;

    /// <summary>
    /// Maximum allowed length for the skill description.
    /// </summary>
    internal const int MaxDescriptionLength = 1024;

    /// <summary>
    /// Maximum allowed length for the compatibility field.
    /// </summary>
    internal const int MaxCompatibilityLength = 500;

    // Validates skill names: lowercase letters, numbers, and hyphens only;
    // must not start or end with a hyphen; must not contain consecutive hyphens.
    private static readonly Regex s_validNameRegex = new("^[a-z0-9]([a-z0-9]*-[a-z0-9])*[a-z0-9]*$", RegexOptions.Compiled);

    /// <summary>
    /// Validates the given frontmatter against specification rules.
    /// </summary>
    /// <param name="frontmatter">The frontmatter to validate.</param>
    /// <param name="reason">When validation fails, contains a human-readable description of the failure.</param>
    /// <returns><see langword="true"/> if the frontmatter is valid; otherwise, <see langword="false"/>.</returns>
    internal static bool Validate(AgentSkillFrontmatter frontmatter, [NotNullWhen(false)] out string? reason)
    {
        return Validate(frontmatter.Name, frontmatter.Description, out reason);
    }

    /// <summary>
    /// Validates raw name and description values against specification rules.
    /// </summary>
    /// <param name="name">The skill name to validate (may be <see langword="null"/>).</param>
    /// <param name="description">The skill description to validate (may be <see langword="null"/>).</param>
    /// <param name="reason">When validation fails, contains a human-readable description of the failure.</param>
    /// <returns><see langword="true"/> if the values are valid; otherwise, <see langword="false"/>.</returns>
    internal static bool Validate(
        string? name,
        string? description,
        [NotNullWhen(false)] out string? reason)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            reason = "Skill name is required.";
            return false;
        }

        if (name.Length > MaxNameLength)
        {
            reason = $"Skill name must be {MaxNameLength} characters or fewer.";
            return false;
        }

        if (!s_validNameRegex.IsMatch(name))
        {
            reason = "Skill name must use only lowercase letters, numbers, and hyphens, and must not start or end with a hyphen or contain consecutive hyphens.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            reason = "Skill description is required.";
            return false;
        }

        if (description.Length > MaxDescriptionLength)
        {
            reason = $"Skill description must be {MaxDescriptionLength} characters or fewer.";
            return false;
        }

        reason = null;
        return true;
    }
}
