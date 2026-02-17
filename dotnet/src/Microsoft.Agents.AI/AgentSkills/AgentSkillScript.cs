// Copyright (c) Microsoft. All rights reserved.

using System;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Agents.AI;

/// <summary>
/// Represents an executable script associated with an Agent Skill.
/// </summary>
/// <remarks>
/// Scripts are executable files that skills can use to perform specialized operations,
/// such as searching external APIs or processing data.
/// </remarks>
public sealed class AgentSkillScript
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AgentSkillScript"/> class.
    /// </summary>
    /// <param name="name">The name of the script as referenced in the skill.</param>
    /// <param name="path">The absolute file path to the script executable.</param>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> or <paramref name="path"/> is <see langword="null"/>.</exception>
    public AgentSkillScript(string name, string path)
    {
        this.Name = Throw.IfNullOrWhitespace(name);
        this.Path = Throw.IfNullOrWhitespace(path);
    }

    /// <summary>
    /// Gets the name of the script as referenced in the skill.
    /// </summary>
    /// <value>
    /// The script name, typically matching the name listed in the skill's frontmatter.
    /// </value>
    public string Name { get; }

    /// <summary>
    /// Gets the absolute file path to the script executable.
    /// </summary>
    /// <value>
    /// The full path to the script file on the filesystem.
    /// </value>
    public string Path { get; }
}
