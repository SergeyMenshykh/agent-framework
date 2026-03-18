// Copyright (c) Microsoft. All rights reserved.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Agents.AI.UnitTests.AgentSkills;

/// <summary>
/// Unit tests for <see cref="AgentCodeSkillsSource"/>.
/// </summary>
public sealed class AgentCodeSkillsSourceTests
{
    [Fact]
    public async Task GetSkillsAsync_ValidSkills_ReturnsAll()
    {
        // Arrange
        var skills = new[]
        {
            new AgentCodeSkill("my-skill", "A valid skill.", "Instructions."),
            new AgentCodeSkill("another", "Another valid skill.", "More instructions."),
        };
        var source = new AgentCodeSkillsSource(skills);

        // Act
        var result = await source.GetSkillsAsync(CancellationToken.None);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("my-skill", result[0].Frontmatter.Name);
        Assert.Equal("another", result[1].Frontmatter.Name);
    }

    [Fact]
    public async Task GetSkillsAsync_InvalidFrontmatter_ExcludesSkill()
    {
        // Arrange
        var skills = new[]
        {
            new AgentCodeSkill("valid-skill", "A valid skill.", "Instructions."),
            new AgentCodeSkill("INVALID-NAME", "An invalid skill.", "Instructions."),
        };
        var source = new AgentCodeSkillsSource(skills);

        // Act
        var result = await source.GetSkillsAsync(CancellationToken.None);

        // Assert
        Assert.Single(result);
        Assert.Equal("valid-skill", result[0].Frontmatter.Name);
    }

    [Fact]
    public async Task GetSkillsAsync_AllInvalid_ReturnsEmpty()
    {
        // Arrange
        var skills = new[]
        {
            new AgentCodeSkill("-leading", "Desc.", "Instructions."),
            new AgentCodeSkill("trailing-", "Desc.", "Instructions."),
        };
        var source = new AgentCodeSkillsSource(skills);

        // Act
        var result = await source.GetSkillsAsync(CancellationToken.None);

        // Assert
        Assert.Empty(result);
    }
}
