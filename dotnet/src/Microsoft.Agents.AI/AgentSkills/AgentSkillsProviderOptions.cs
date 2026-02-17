// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;

namespace Microsoft.Agents.AI;

/// <summary>
/// Configuration options for the <see cref="AgentSkillsProvider"/>.
/// </summary>
public sealed class AgentSkillsProviderOptions
{
    /// <summary>
    /// Gets or sets the directories to search for skills.
    /// </summary>
    /// <value>
    /// A list of directory paths where skills are located. Each directory may contain
    /// multiple skill subdirectories, each with a SKILL.md file.
    /// Default is an empty list.
    /// </value>
    public IList<string> SkillDirectories { get; set; } = [];

    /// <summary>
    /// Gets or sets the key used to store provider state in the session state bag.
    /// </summary>
    /// <value>
    /// The state key. Default is "AgentSkillsProvider".
    /// </value>
    public string? StateKey { get; set; }

    /// <summary>
    /// Gets or sets the timeout for script execution in milliseconds.
    /// </summary>
    /// <value>
    /// The timeout in milliseconds. Default is 30000 (30 seconds).
    /// Set to -1 for no timeout (not recommended).
    /// </value>
    public int ScriptTimeoutMilliseconds { get; set; } = 30000;

    /// <summary>
    /// Gets or sets the system prompt template for advertising available skills.
    /// </summary>
    /// <value>
    /// The prompt template. Use {0} for the skills list placeholder.
    /// Default provides a comprehensive skills introduction.
    /// </value>
    public string? SkillsInstructionPrompt { get; set; }

    /// <summary>
    /// Gets the default skills instruction prompt.
    /// </summary>
    internal static string DefaultSkillsInstructionPrompt => @"# Skills

You have access to skills that extend your capabilities. Skills are modular packages
containing instructions, resources, and scripts for specialized tasks.

## Available Skills

The following skills are available to you. Use them when relevant to the task:

{0}

## How to Use Skills

**Progressive disclosure**: Load skill information only when needed.

1. **When a skill is relevant to the current task**: Use `load_skill(skill_name)` to read the full instructions.
2. **For additional documentation**: Use `read_skill_resource(skill_name, resource_name)` to read supplementary resources.
3. **To execute skill scripts**: Use `run_skill_script(skill_name, script_name, args)` with appropriate command-line arguments.

**Best practices**:
- Select skills based on task relevance and descriptions listed above
- Use progressive disclosure: load only what you need, when you need it, starting with load_skill
- Follow the skill's documented usage patterns and examples";
}
