// Copyright (c) Microsoft. All rights reserved.

using System;
using Microsoft.Extensions.AI;

namespace Microsoft.Agents.AI.UnitTests.AgentSkills;

/// <summary>
/// Unit tests for <see cref="AgentInlineSkill"/>.
/// </summary>
public sealed class AgentInlineSkillTests
{
    [Fact]
    public void Constructor_WithNameAndDescription_SetsFrontmatter()
    {
        // Arrange & Act
        var skill = new AgentInlineSkill("my-skill", "A valid skill.", "Instructions.");

        // Assert
        Assert.Equal("my-skill", skill.Frontmatter.Name);
        Assert.Equal("A valid skill.", skill.Frontmatter.Description);
        Assert.Null(skill.Frontmatter.License);
        Assert.Null(skill.Frontmatter.Compatibility);
        Assert.Null(skill.Frontmatter.AllowedTools);
        Assert.Null(skill.Frontmatter.Metadata);
    }

    [Fact]
    public void Constructor_WithAllProps_SetsFrontmatter()
    {
        // Arrange
        var metadata = new AdditionalPropertiesDictionary { ["key"] = "value" };

        // Act
        var skill = new AgentInlineSkill(
            "my-skill",
            "A valid skill.",
            "Instructions.",
            license: "MIT",
            compatibility: "gpt-4",
            allowedTools: "tool-a tool-b",
            metadata: metadata);

        // Assert
        Assert.Equal("my-skill", skill.Frontmatter.Name);
        Assert.Equal("A valid skill.", skill.Frontmatter.Description);
        Assert.Equal("MIT", skill.Frontmatter.License);
        Assert.Equal("gpt-4", skill.Frontmatter.Compatibility);
        Assert.Equal("tool-a tool-b", skill.Frontmatter.AllowedTools);
        Assert.NotNull(skill.Frontmatter.Metadata);
        Assert.Equal("value", skill.Frontmatter.Metadata["key"]);
    }

    [Fact]
    public void Constructor_WithFrontmatter_UsesFrontmatterDirectly()
    {
        // Arrange
        var frontmatter = new AgentSkillFrontmatter("my-skill", "A valid skill.")
        {
            License = "Apache-2.0",
            Compatibility = "gpt-4",
            AllowedTools = "tool-a",
            Metadata = new AdditionalPropertiesDictionary { ["env"] = "prod" },
        };

        // Act
        var skill = new AgentInlineSkill(frontmatter, "Instructions.");

        // Assert
        Assert.Same(frontmatter, skill.Frontmatter);
        Assert.Equal("Apache-2.0", skill.Frontmatter.License);
        Assert.Equal("gpt-4", skill.Frontmatter.Compatibility);
        Assert.Equal("tool-a", skill.Frontmatter.AllowedTools);
        Assert.Equal("prod", skill.Frontmatter.Metadata!["env"]);
    }

    [Fact]
    public void Constructor_WithFrontmatter_NullFrontmatter_Throws()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new AgentInlineSkill(null!, "Instructions."));
    }

    [Fact]
    public void Constructor_WithFrontmatter_NullInstructions_Throws()
    {
        // Arrange
        var frontmatter = new AgentSkillFrontmatter("my-skill", "A valid skill.");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new AgentInlineSkill(frontmatter, null!));
    }

    [Fact]
    public void Constructor_WithAllProps_NullInstructions_Throws()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new AgentInlineSkill("my-skill", "A valid skill.", null!));
    }
}
