// Copyright (c) Microsoft. All rights reserved.

using System;

namespace Microsoft.Agents.AI.UnitTests.AgentSkills;

/// <summary>
/// Unit tests for <see cref="AgentSkillFrontmatterValidator"/>.
/// </summary>
public sealed class AgentSkillFrontmatterValidatorTests
{
    [Theory]
    [InlineData("my-skill")]
    [InlineData("a")]
    [InlineData("skill123")]
    [InlineData("a1b2c3")]
    public void Validate_ValidFrontmatter_ReturnsTrue(string name)
    {
        // Arrange
        var frontmatter = new AgentSkillFrontmatter(name, "A valid description.");

        // Act
        bool result = AgentSkillFrontmatterValidator.Validate(frontmatter, out string? reason);

        // Assert
        Assert.True(result);
        Assert.Null(reason);
    }

    [Theory]
    [InlineData("-leading-hyphen")]
    [InlineData("trailing-hyphen-")]
    [InlineData("has spaces")]
    [InlineData("UPPERCASE")]
    [InlineData("consecutive--hyphens")]
    [InlineData("special!chars")]
    public void Validate_InvalidName_ReturnsFalse(string name)
    {
        // Arrange
        var frontmatter = new AgentSkillFrontmatter(name, "A valid description.");

        // Act
        bool result = AgentSkillFrontmatterValidator.Validate(frontmatter, out string? reason);

        // Assert
        Assert.False(result);
        Assert.NotNull(reason);
        Assert.Contains("name", reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_NameExceedsMaxLength_ReturnsFalse()
    {
        // Arrange
        string longName = new('a', 65);
        var frontmatter = new AgentSkillFrontmatter(longName, "A valid description.");

        // Act
        bool result = AgentSkillFrontmatterValidator.Validate(frontmatter, out string? reason);

        // Assert
        Assert.False(result);
        Assert.NotNull(reason);
    }

    [Fact]
    public void Validate_DescriptionExceedsMaxLength_ReturnsFalse()
    {
        // Arrange
        string longDesc = new('x', 1025);
        var frontmatter = new AgentSkillFrontmatter("valid-name", longDesc);

        // Act
        bool result = AgentSkillFrontmatterValidator.Validate(frontmatter, out string? reason);

        // Assert
        Assert.False(result);
        Assert.NotNull(reason);
    }

    [Fact]
    public void Validate_StringOverload_ValidValues_ReturnsTrue()
    {
        // Act
        bool result = AgentSkillFrontmatterValidator.Validate("my-skill", "A valid description.", out string? reason);

        // Assert
        Assert.True(result);
        Assert.Null(reason);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_StringOverload_NullOrWhitespaceName_ReturnsFalse(string? name)
    {
        // Act
        bool result = AgentSkillFrontmatterValidator.Validate(name, "A valid description.", out string? reason);

        // Assert
        Assert.False(result);
        Assert.NotNull(reason);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_StringOverload_NullOrWhitespaceDescription_ReturnsFalse(string? description)
    {
        // Act
        bool result = AgentSkillFrontmatterValidator.Validate("valid-name", description, out string? reason);

        // Assert
        Assert.False(result);
        Assert.NotNull(reason);
    }

    [Fact]
    public void Validate_StringOverload_InvalidName_ReturnsFalse()
    {
        // Act
        bool result = AgentSkillFrontmatterValidator.Validate("UPPERCASE", "A valid description.", out string? reason);

        // Assert
        Assert.False(result);
        Assert.NotNull(reason);
    }
}
