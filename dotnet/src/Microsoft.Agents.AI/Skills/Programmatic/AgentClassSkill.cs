// Copyright (c) Microsoft. All rights reserved.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Agents.AI;

/// <summary>
/// Abstract base class for defining skills as C# classes that bundle all components together.
/// </summary>
/// <remarks>
/// <para>
/// Inherit from this class to create a self-contained skill definition. Override the abstract
/// properties to provide name, description, and instructions.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class PdfFormatterSkill : AgentClassSkill
/// {
///     public override AgentSkillFrontmatter Frontmatter { get; } = new("pdf-formatter", "Format documents as PDF.");
///     public override string Instructions =&gt; "Use this skill to format documents...";
///
///     public override IReadOnlyList&lt;AgentSkillResource&gt;? Resources { get; } =
///     [
///         new AgentInlineSkillResource("Use this template...", "template"),
///     ];
///
///     public override IReadOnlyList&lt;AgentSkillScript&gt;? Scripts { get; } =
///     [
///         new AgentInlineSkillScript(FormatPdf, "format-pdf"),
///     ];
///
///     private static string FormatPdf(string content) =&gt; content;
/// }
/// </code>
/// </example>
[Experimental(DiagnosticIds.Experiments.AgentsAIExperiments)]
public abstract class AgentClassSkill : AgentSkill
{
    /// <summary>
    /// Gets the raw instructions text for this skill.
    /// </summary>
    public abstract string Instructions { get; }

    /// <inheritdoc/>
    /// <remarks>
    /// Returns a synthesized XML document containing name, description, instructions, resources, and scripts.
    /// Override to provide custom content.
    /// </remarks>
    public override string Content => SkillContentBuilder.BuildContent(this.Frontmatter.Name, this.Frontmatter.Description, SkillContentBuilder.BuildBody(this.Instructions, this.Resources, this.Scripts));
}
