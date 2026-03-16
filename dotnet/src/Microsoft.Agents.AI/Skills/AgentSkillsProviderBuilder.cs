// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Agents.AI;

/// <summary>
/// Fluent builder for constructing an <see cref="AgentSkillsProvider"/> backed by a composite source.
/// </summary>
/// <remarks>
/// <para>
/// Use this builder to combine multiple heterogeneous skill sources into a single provider:
/// </para>
/// <code>
/// var provider = new AgentSkillsProviderBuilder()
///     .AddFileSource("/path/to/skills")
///     .AddCodeSkills(myCodeSkill1, myCodeSkill2)
///     .AddClassSkills(new PdfFormatterSkill())
///     .Build();
/// </code>
/// </remarks>
[Experimental(DiagnosticIds.Experiments.AgentsAIExperiments)]
public sealed class AgentSkillsProviderBuilder
{
    private readonly List<AgentSkillsSource> _sources = [];
    private readonly List<Func<FileSkillScriptExecutor?, ILoggerFactory?, FileAgentSkillsSource>> _fileSourceFactories = [];
    private AgentSkillsProviderOptions? _options;
    private ILoggerFactory? _loggerFactory;
    private FileSkillScriptExecutor? _scriptExecutor;

    /// <summary>
    /// Adds a file-based skill source that discovers skills from filesystem directories.
    /// </summary>
    /// <param name="skillPath">Path to search for skills.</param>
    /// <param name="scriptExecutor">Optional executor for file-based scripts. Falls back to the executor set via <see cref="WithFileScriptExecutor"/>.</param>
    /// <param name="allowedResourceExtensions">Optional resource extension filter.</param>
    /// <returns>This builder instance for chaining.</returns>
    public AgentSkillsProviderBuilder AddFileSkills(string skillPath, FileSkillScriptExecutor? scriptExecutor = null, IEnumerable<string>? allowedResourceExtensions = null)
    {
        this.AddFileSkills([skillPath], scriptExecutor, allowedResourceExtensions);
        return this;
    }

    /// <summary>
    /// Adds a file-based skill source that discovers skills from multiple filesystem directories.
    /// </summary>
    /// <param name="skillPaths">Paths to search for skills.</param>
    /// <param name="scriptExecutor">Optional executor for file-based scripts. Falls back to the executor set via <see cref="WithFileScriptExecutor"/>.</param>
    /// <param name="allowedResourceExtensions">Optional resource extension filter.</param>
    /// <returns>This builder instance for chaining.</returns>
    public AgentSkillsProviderBuilder AddFileSkills(IEnumerable<string> skillPaths, FileSkillScriptExecutor? scriptExecutor = null, IEnumerable<string>? allowedResourceExtensions = null)
    {
        this._fileSourceFactories.Add((builderExecutor, loggerFactory) =>
            new FileAgentSkillsSource(skillPaths, allowedResourceExtensions, loggerFactory, scriptExecutor ?? builderExecutor));
        return this;
    }

    /// <summary>
    /// Adds code-defined skills.
    /// </summary>
    /// <param name="skills">The code-defined skills to add.</param>
    /// <returns>This builder instance for chaining.</returns>
    public AgentSkillsProviderBuilder AddCodeSkills(params AgentCodeSkill[] skills)
    {
        this._sources.Add(new CodeAgentSkillsSource(skills));
        return this;
    }

    /// <summary>
    /// Adds code-defined skills with an optional source-level filter.
    /// </summary>
    /// <param name="skills">The code-defined skills to add.</param>
    /// <returns>This builder instance for chaining.</returns>
    public AgentSkillsProviderBuilder AddCodeSkills(IEnumerable<AgentCodeSkill> skills)
    {
        var source = new CodeAgentSkillsSource(skills);
        this._sources.Add(source);
        return this;
    }

    /// <summary>
    /// Adds class-based skills.
    /// </summary>
    /// <param name="skills">The class-based skills to add.</param>
    /// <returns>This builder instance for chaining.</returns>
    public AgentSkillsProviderBuilder AddClassSkills(params AgentClassSkill[] skills)
    {
        this._sources.Add(new ClassAgentSkillsSource(skills));
        return this;
    }

    /// <summary>
    /// Adds a custom skill source.
    /// </summary>
    /// <param name="source">The custom skill source.</param>
    /// <returns>This builder instance for chaining.</returns>
    public AgentSkillsProviderBuilder AddSource(AgentSkillsSource source)
    {
        this._sources.Add(source ?? throw new ArgumentNullException(nameof(source)));
        return this;
    }

    /// <summary>
    /// Sets a custom system prompt template.
    /// </summary>
    /// <param name="promptTemplate">The prompt template with <c>{0}</c> placeholder for skills list.</param>
    /// <returns>This builder instance for chaining.</returns>
    public AgentSkillsProviderBuilder WithPromptTemplate(string promptTemplate)
    {
        this.EnsureOptions().SkillsInstructionPrompt = promptTemplate;
        return this;
    }

    /// <summary>
    /// Enables or disables the script approval gate.
    /// </summary>
    /// <param name="enabled">Whether script execution requires approval.</param>
    /// <returns>This builder instance for chaining.</returns>
    public AgentSkillsProviderBuilder WithScriptApproval(bool enabled = true)
    {
        this.EnsureOptions().ScriptApproval = enabled;
        return this;
    }

    /// <summary>
    /// Sets the executor for file-based skill scripts.
    /// </summary>
    /// <param name="executor">The delegate that executes file-based scripts.</param>
    /// <returns>This builder instance for chaining.</returns>
    public AgentSkillsProviderBuilder WithFileScriptExecutor(FileSkillScriptExecutor executor)
    {
        this._scriptExecutor = executor ?? throw new ArgumentNullException(nameof(executor));
        return this;
    }

    /// <summary>
    /// Sets the logger factory.
    /// </summary>
    /// <param name="loggerFactory">The logger factory.</param>
    /// <returns>This builder instance for chaining.</returns>
    public AgentSkillsProviderBuilder WithLoggerFactory(ILoggerFactory loggerFactory)
    {
        this._loggerFactory = loggerFactory;
        return this;
    }

    /// <summary>
    /// Builds the <see cref="AgentSkillsProvider"/>.
    /// </summary>
    /// <returns>A configured <see cref="AgentSkillsProvider"/> backed by a composite source.</returns>
    public AgentSkillsProvider Build()
    {
        foreach (var factory in this._fileSourceFactories)
        {
            this._sources.Add(factory(this._scriptExecutor, this._loggerFactory));
        }

        AgentSkillsSource source;
        if (this._sources.Count == 1)
        {
            source = this._sources[0];
        }
        else
        {
            source = new CompositeAgentSkillsSource(this._sources);
        }

        return new AgentSkillsProvider(source, this._options, this._loggerFactory);
    }

    private AgentSkillsProviderOptions EnsureOptions()
    {
        if (this._options == null)
        {
            this._options = new AgentSkillsProviderOptions();
        }

        return this._options;
    }
}
