// Copyright (c) Microsoft. All rights reserved.

using System;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Agents.AI;

/// <summary>
/// Represents a resource file associated with an Agent Skill.
/// </summary>
/// <remarks>
/// Resources are supplementary files (e.g., REFERENCE.md, FORMS.md) that provide additional
/// documentation or context for a skill. They are loaded only when the agent explicitly requests them.
/// </remarks>
public sealed class AgentSkillResource
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AgentSkillResource"/> class.
    /// </summary>
    /// <param name="name">The name of the resource file (e.g., "REFERENCE.md").</param>
    /// <param name="content">The textual content of the resource file.</param>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> or <paramref name="content"/> is <see langword="null"/>.</exception>
    public AgentSkillResource(string name, string content)
    {
        this.Name = Throw.IfNullOrWhitespace(name);
        this.Content = Throw.IfNull(content);
    }

    /// <summary>
    /// Gets the name of the resource file.
    /// </summary>
    /// <value>
    /// The file name, typically matching the name listed in the skill's frontmatter (e.g., "REFERENCE.md").
    /// </value>
    public string Name { get; }

    /// <summary>
    /// Gets the textual content of the resource file.
    /// </summary>
    /// <value>
    /// The full content of the resource file, typically markdown text.
    /// </value>
    public string Content { get; }
}
