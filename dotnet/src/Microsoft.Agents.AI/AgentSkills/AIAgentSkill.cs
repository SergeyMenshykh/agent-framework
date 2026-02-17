// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Agents.AI;

/// <summary>
/// Represents an Agent Skill as defined by the <see href="https://agentskills.io/specification">Agent Skills specification</see>.
/// </summary>
/// <remarks>
/// <para>
/// A skill is a modular package of instructions, resources, and scripts that enables AI agents
/// to perform specialized tasks. Skills follow the progressive disclosure pattern:
/// <list type="number">
/// <item><description><strong>Metadata</strong> (~100 tokens): Name and description are always available for skill discovery.</description></item>
/// <item><description><strong>Instructions</strong> (&lt; 5000 tokens recommended): Full instructions loaded when the skill is activated.</description></item>
/// <item><description><strong>Resources</strong> (as needed): Supplementary files loaded only when required.</description></item>
/// <item><description><strong>Scripts</strong> (as needed): Executable scripts invoked only when required.</description></item>
/// </list>
/// </para>
/// <para>
/// Skills are discovered from filesystem directories containing a <c>SKILL.md</c> file with YAML frontmatter
/// and markdown body containing the skill instructions.
/// </para>
/// </remarks>
public sealed class AIAgentSkill
{
    private const int MaxNameLength = 64;
    private const int MaxDescriptionLength = 1024;
    private const int MaxCompatibilityLength = 500;

    /// <summary>
    /// Initializes a new instance of the <see cref="AIAgentSkill"/> class.
    /// </summary>
    /// <param name="name">
    /// The skill name. Must be a unique identifier.
    /// </param>
    /// <param name="description">A description of what the skill does and when to use it. Must be 1–1024 characters.</param>
    /// <param name="content">The main instructional content (the body of SKILL.md).</param>
    /// <param name="resources">Optional list of resources associated with the skill.</param>
    /// <param name="scripts">Optional list of scripts associated with the skill.</param>
    /// <param name="compatibility">Optional environment requirements (max 500 characters).</param>
    /// <param name="metadata">Optional additional metadata as key-value pairs.</param>
    /// <param name="sourcePath">Optional path indicating the skill's source location.</param>
    /// <exception cref="ArgumentNullException"><paramref name="name"/>, <paramref name="description"/>, or <paramref name="content"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="name"/>, <paramref name="description"/>, or <paramref name="compatibility"/> exceeds the maximum length.
    /// </exception>
    public AIAgentSkill(
        string name,
        string description,
        string content,
        IReadOnlyList<AgentSkillResource>? resources = null,
        IReadOnlyList<AgentSkillScript>? scripts = null,
        string? compatibility = null,
        IReadOnlyDictionary<string, string>? metadata = null,
        string? sourcePath = null)
    {
        _ = Throw.IfNullOrWhitespace(name);
        _ = Throw.IfNullOrWhitespace(description);
        _ = Throw.IfNull(content);

        if (name.Length > MaxNameLength)
        {
            throw new ArgumentException($"Skill name exceeds {MaxNameLength} characters ({name.Length} chars).", nameof(name));
        }

        if (description.Length > MaxDescriptionLength)
        {
            throw new ArgumentException($"Skill description exceeds {MaxDescriptionLength} characters ({description.Length} chars).", nameof(description));
        }

        if (compatibility?.Length > MaxCompatibilityLength)
        {
            throw new ArgumentException($"Skill compatibility exceeds {MaxCompatibilityLength} characters ({compatibility.Length} chars).", nameof(compatibility));
        }

        this.Name = name;
        this.Description = description;
        this.Content = content;
        this.Resources = resources ?? [];
        this.Scripts = scripts ?? [];
        this.Compatibility = compatibility;
        this.Metadata = metadata;
        this.SourcePath = sourcePath;
    }

    /// <summary>
    /// Gets the skill name.
    /// </summary>
    /// <value>
    /// A unique identifier for the skill.
    /// </value>
    public string Name { get; }

    /// <summary>
    /// Gets the description of what the skill does and when to use it.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Gets the main instructional content of the skill.
    /// </summary>
    /// <value>
    /// The Markdown body from the SKILL.md file, containing step-by-step instructions,
    /// examples, and guidance for the agent.
    /// </value>
    public string Content { get; }

    /// <summary>
    /// Gets the list of resources associated with the skill.
    /// </summary>
    public IReadOnlyList<AgentSkillResource> Resources { get; }

    /// <summary>
    /// Gets the list of scripts associated with the skill.
    /// </summary>
    public IReadOnlyList<AgentSkillScript> Scripts { get; }

    /// <summary>
    /// Gets the optional environment requirements or compatibility information.
    /// </summary>
    public string? Compatibility { get; }

    /// <summary>
    /// Gets the optional additional metadata as key-value pairs.
    /// </summary>
    public IReadOnlyDictionary<string, string>? Metadata { get; }

    /// <summary>
    /// Gets the optional path or location indicating the skill's source (e.g., the directory path for filesystem-based skills).
    /// </summary>
    public string? SourcePath { get; }
}
