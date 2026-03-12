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
/// properties to provide name, description, body, resources, and scripts. Users instantiating
/// the class do not need to provide these — they are part of the class definition.
/// </para>
/// <para>
/// Metadata can be customized at instantiation time via <see cref="DescriptionOverride"/>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class PdfFormatterSkill : AgentClassSkill
/// {
///     public override string Name =&gt; "pdf-formatter";
///     public override string Description =&gt; "Format documents as PDF.";
///     public override string Body =&gt; "Use this skill to format documents...";
///     public override IReadOnlyList&lt;AgentSkillResource&gt; Resources =&gt; Array.Empty&lt;AgentSkillResource&gt;();
///     public override IReadOnlyList&lt;AgentSkillScript&gt; Scripts =&gt; Array.Empty&lt;AgentSkillScript&gt;();
/// }
/// </code>
/// </example>
[Experimental(DiagnosticIds.Experiments.AgentsAIExperiments)]
public abstract class AgentClassSkill : AgentSkill
{
    /// <summary>
    /// Gets or sets an optional description override, allowing customization at instantiation time.
    /// When set, this value is returned instead of the class-defined <see cref="AgentSkill.Description"/>.
    /// </summary>
    public string? DescriptionOverride { get; set; }
}
