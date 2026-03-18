// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Agents.AI;

/// <summary>
/// A skill source that holds class-based <see cref="AgentClassSkill"/> instances.
/// </summary>
/// <remarks>
/// Skills with invalid frontmatter are excluded at load time and the reason is logged.
/// </remarks>
[Experimental(DiagnosticIds.Experiments.AgentsAIExperiments)]
public sealed partial class AgentClassSkillsSource : AgentSkillsSource
{
    private readonly IReadOnlyList<AgentClassSkill> _skills;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentClassSkillsSource"/> class.
    /// </summary>
    /// <param name="skills">The class-based skills to include in this source.</param>
    /// <param name="loggerFactory">Optional logger factory for diagnostics.</param>
    public AgentClassSkillsSource(IEnumerable<AgentClassSkill> skills, ILoggerFactory? loggerFactory = null)
    {
        _ = Throw.IfNull(skills);
        this._logger = (loggerFactory ?? NullLoggerFactory.Instance).CreateLogger<AgentClassSkillsSource>();
        this._skills = skills.ToList();
    }

    /// <inheritdoc/>
    public override Task<IReadOnlyList<AgentSkill>> GetSkillsAsync(CancellationToken cancellationToken = default)
    {
        var result = new List<AgentSkill>();
        foreach (var skill in this._skills)
        {
            if (AgentSkillFrontmatterValidator.Validate(skill.Frontmatter, out string? reason))
            {
                result.Add(skill);
            }
            else
            {
                LogInvalidFrontmatter(this._logger, skill.Frontmatter.Name, reason);
            }
        }

        return Task.FromResult<IReadOnlyList<AgentSkill>>(result);
    }

    [LoggerMessage(LogLevel.Error, "Class-based skill '{SkillName}' has invalid frontmatter and was excluded: {Reason}")]
    private static partial void LogInvalidFrontmatter(ILogger logger, string skillName, string reason);
}
