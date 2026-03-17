// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Agents.AI;

/// <summary>
/// Abstract base class for defining skills as C# classes that bundle all components together.
/// </summary>
/// <remarks>
/// <para>
/// Inherit from this class to create a self-contained skill definition. Override the abstract
/// properties to provide name, description, and body. Override <see cref="Resources"/> and
/// <see cref="Scripts"/> to register resources and scripts.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class PdfFormatterSkill : AgentClassSkill
/// {
///     public override string Name =&gt; "pdf-formatter";
///     public override string Description =&gt; "Format documents as PDF.";
///     public override string Body =&gt; "Use this skill to format documents...";
///
///     public override IReadOnlyList&lt;AgentSkillResource&gt;? Resources { get; } =
///     [
///         new AgentCodeSkillResource("template", "Use this template..."),
///     ];
///
///     public override IReadOnlyList&lt;AgentSkillScript&gt;? Scripts { get; } =
///     [
///         new AgentCodeSkillScript(FormatPdf, "format-pdf"),
///     ];
///
///     private static string FormatPdf(string content) =&gt; content;
/// }
/// </code>
/// </example>
[Experimental(DiagnosticIds.Experiments.AgentsAIExperiments)]
public abstract class AgentClassSkill : AgentSkill
{
    /// <inheritdoc/>
    /// <remarks>
    /// Returns a synthesized document containing YAML frontmatter (name and description)
    /// followed by the body. Override to provide custom content.
    /// </remarks>
    public override string Content => $"---\nname: {this.Name}\ndescription: {this.Description}\n---\n{this.Body}";

    /// <inheritdoc/>
    public override IReadOnlyList<AgentSkillResource>? Resources => null;

    /// <inheritdoc/>
    public override IReadOnlyList<AgentSkillScript>? Scripts => null;
}
