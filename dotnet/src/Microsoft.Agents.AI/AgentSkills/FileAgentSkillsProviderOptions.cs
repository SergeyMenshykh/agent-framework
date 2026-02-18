// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;

namespace Microsoft.Agents.AI;

/// <summary>
/// Configuration options for <see cref="FileAgentSkillsProvider"/>.
/// </summary>
public sealed class FileAgentSkillsProviderOptions
{
    /// <summary>
    /// Gets or sets a custom system prompt template for advertising skills.
    /// Use <c>{0}</c> as the placeholder for the generated skills list.
    /// When <see langword="null"/>, a default template is used.
    /// </summary>
    public string? SkillsInstructionPrompt { get; set; }

    /// <summary>
    /// Gets or sets the allowed file extensions for skill resources (e.g., ".md", ".json").
    /// When <see langword="null"/>, all extensions are permitted.
    /// </summary>
    public IEnumerable<string>? AllowedResourceExtensions { get; set; }

    /// <summary>
    /// Gets or sets the allowed path prefixes for skill resource references.
    /// Each entry is matched as a segment-aligned prefix against the resource's relative path.
    /// <para>Examples:</para>
    /// <list type="bullet">
    /// <item><description><c>"references"</c> — allows <c>references/FAQ.md</c>, <c>references/api/v2/schema.json</c>, etc.</description></item>
    /// <item><description><c>"references/public"</c> — allows <c>references/public/doc.md</c> but not <c>references/internal/doc.md</c>.</description></item>
    /// <item><description><c>"data/schemas"</c> — allows <c>data/schemas/v1/input.json</c> but not <c>data/exports/dump.csv</c>.</description></item>
    /// </list>
    /// When <see langword="null"/>, all paths within the skill directory are permitted.
    /// </summary>
    public IEnumerable<string>? AllowedResourcePaths { get; set; }
}
