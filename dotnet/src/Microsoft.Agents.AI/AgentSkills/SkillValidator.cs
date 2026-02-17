// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Agents.AI;

/// <summary>
/// Validates that Agent Skills have all required files and resources.
/// </summary>
/// <remarks>
/// Validates that all resources, scripts, and other files referenced in a skill's
/// SKILL.md frontmatter actually exist on the filesystem.
/// </remarks>
internal static class SkillValidator
{
    /// <summary>
    /// Represents the result of validating a skill.
    /// </summary>
    public sealed class ValidationResult
    {
        /// <summary>
        /// Gets a value indicating whether the skill is valid.
        /// </summary>
        public bool IsValid => this.Errors.Count == 0;

        /// <summary>
        /// Gets the list of validation errors.
        /// </summary>
        public List<string> Errors { get; } = [];

        /// <summary>
        /// Adds an error to the validation result.
        /// </summary>
        /// <param name="error">The error message.</param>
        public void AddError(string error)
        {
            this.Errors.Add(error);
        }

        /// <summary>
        /// Gets a formatted error message with all validation errors.
        /// </summary>
        /// <returns>A formatted error message.</returns>
        public string GetErrorMessage()
        {
            if (this.IsValid)
            {
                return string.Empty;
            }

            var sb = new StringBuilder();
            sb.AppendLine("Skill validation failed:");
            foreach (string error in this.Errors)
            {
                sb.AppendLine($"  - {error}");
            }
            return sb.ToString();
        }
    }

    /// <summary>
    /// Validates a skill by checking that all referenced files exist.
    /// </summary>
    /// <param name="skill">The skill to validate.</param>
    /// <returns>A <see cref="ValidationResult"/> indicating whether the skill is valid.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="skill"/> is <see langword="null"/>.</exception>
    public static ValidationResult Validate(AIAgentSkill skill)
    {
        _ = Throw.IfNull(skill);

        var result = new ValidationResult();

        if (string.IsNullOrWhiteSpace(skill.SourcePath))
        {
            result.AddError("Skill source path is null or empty.");
            return result;
        }

        if (!Directory.Exists(skill.SourcePath))
        {
            result.AddError($"Skill source directory does not exist: {skill.SourcePath}");
            return result;
        }

        // Validate that resources exist
        ValidateResources(skill, result);

        // Validate that scripts exist
        ValidateScripts(skill, result);

        return result;
    }

    /// <summary>
    /// Validates a skill by parsing its SKILL.md file and checking referenced files.
    /// </summary>
    /// <param name="skillFilePath">The path to the SKILL.md file.</param>
    /// <param name="skillDirectoryPath">The directory containing the skill.</param>
    /// <returns>A <see cref="ValidationResult"/> indicating whether the skill is valid.</returns>
    public static ValidationResult ValidateFromFile(string skillFilePath, string skillDirectoryPath)
    {
        var result = new ValidationResult();

        try
        {
            AIAgentSkill skill = SkillMarkdownParser.Parse(skillFilePath, skillDirectoryPath);
            return Validate(skill);
        }
        catch (Exception ex)
        {
            result.AddError($"Failed to parse SKILL.md: {ex.Message}");
            return result;
        }
    }

    private static void ValidateResources(AIAgentSkill skill, ValidationResult result)
    {
        if (skill.Resources is null || skill.Resources.Count == 0)
        {
            return;
        }

        // Resources are already loaded from the filesystem during parsing
        // Just verify they have content
        foreach (var resource in skill.Resources)
        {
            if (string.IsNullOrEmpty(resource.Content))
            {
                result.AddError($"Resource '{resource.Name}' has empty content");
            }
        }
    }

    private static void ValidateScripts(AIAgentSkill skill, ValidationResult result)
    {
        if (skill.Scripts is null || skill.Scripts.Count == 0)
        {
            return;
        }

        foreach (var script in skill.Scripts)
        {
            if (!File.Exists(script.Path))
            {
                result.AddError($"Script file not found: {script.Name} (expected at: {script.Path})");
            }
        }
    }
}
