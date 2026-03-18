// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Agents.AI.UnitTests.AgentSkills;

/// <summary>
/// Unit tests for <see cref="AgentClassSkill"/> and <see cref="AgentClassSkillsSource"/>.
/// </summary>
public sealed class AgentClassSkillTests
{
    [Fact]
    public void Resources_DefaultsToNull_WhenNotOverridden()
    {
        // Arrange
        var skill = new MinimalClassSkill();

        // Act & Assert
        Assert.Null(skill.Resources);
    }

    [Fact]
    public void Scripts_DefaultsToNull_WhenNotOverridden()
    {
        // Arrange
        var skill = new MinimalClassSkill();

        // Act & Assert
        Assert.Null(skill.Scripts);
    }

    [Fact]
    public void Resources_ReturnsOverriddenList_WhenOverridden()
    {
        // Arrange
        var skill = new FullClassSkill();

        // Act
        var resources = skill.Resources;

        // Assert
        Assert.Single(resources!);
        Assert.Equal("test-resource", resources![0].Name);
    }

    [Fact]
    public void Scripts_ReturnsOverriddenList_WhenOverridden()
    {
        // Arrange
        var skill = new FullClassSkill();

        // Act
        var scripts = skill.Scripts;

        // Assert
        Assert.Single(scripts!);
        Assert.Equal("TestScript", scripts![0].Name);
    }

    [Fact]
    public void Name_Content_ReturnClassDefinedValues()
    {
        // Arrange
        var skill = new MinimalClassSkill();

        // Act & Assert
        Assert.Equal("minimal", skill.Frontmatter.Name);
        Assert.Contains("<instructions>", skill.Content);
        Assert.Contains("Minimal skill body.", skill.Content);
        Assert.Contains("</instructions>", skill.Content);
    }

    [Fact]
    public void Content_ReturnsSynthesizedXmlDocument()
    {
        // Arrange
        var skill = new MinimalClassSkill();

        // Act & Assert
        Assert.Contains("<name>minimal</name>", skill.Content);
        Assert.Contains("<description>A minimal skill.</description>", skill.Content);
        Assert.Contains("<instructions>", skill.Content);
        Assert.Contains("Minimal skill body.", skill.Content);
    }

    [Fact]
    public async Task AgentClassSkillsSource_ReturnsAllSkills()
    {
        // Arrange
        var skills = new AgentClassSkill[] { new MinimalClassSkill(), new FullClassSkill() };
        var source = new AgentClassSkillsSource(skills);

        // Act
        var result = await source.GetSkillsAsync(CancellationToken.None);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("minimal", result[0].Frontmatter.Name);
        Assert.Equal("full", result[1].Frontmatter.Name);
    }

    [Fact]
    public async Task AgentClassSkillsSource_ExcludesSkillsWithInvalidFrontmatter()
    {
        // Arrange
        var skills = new AgentClassSkill[] { new MinimalClassSkill(), new InvalidNameClassSkill() };
        var source = new AgentClassSkillsSource(skills);

        // Act
        var result = await source.GetSkillsAsync(CancellationToken.None);

        // Assert
        Assert.Single(result);
        Assert.Equal("minimal", result[0].Frontmatter.Name);
    }

    [Fact]
    public void SkillWithOnlyResources_HasNullScripts()
    {
        // Arrange
        var skill = new ResourceOnlySkill();

        // Act & Assert
        Assert.Single(skill.Resources!);
        Assert.Null(skill.Scripts);
    }

    [Fact]
    public void SkillWithOnlyScripts_HasNullResources()
    {
        // Arrange
        var skill = new ScriptOnlySkill();

        // Act & Assert
        Assert.Null(skill.Resources);
        Assert.Single(skill.Scripts!);
    }

    #region Test skill classes

    private sealed class MinimalClassSkill : AgentClassSkill
    {
        public override AgentSkillFrontmatter Frontmatter { get; } = new("minimal", "A minimal skill.");

        public override string Instructions => "Minimal skill body.";
    }

    private sealed class FullClassSkill : AgentClassSkill
    {
        public override AgentSkillFrontmatter Frontmatter { get; } = new("full", "A full skill with resources and scripts.");

        public override string Instructions => "Full skill body.";

        public override IReadOnlyList<AgentSkillResource>? Resources { get; } =
        [
            new AgentCodeSkillResource("test-resource", "resource content"),
        ];

        public override IReadOnlyList<AgentSkillScript>? Scripts { get; } =
        [
            new AgentCodeSkillScript(TestScript, "TestScript"),
        ];

        private static string TestScript(double value) =>
            JsonSerializer.Serialize(new { result = value * 2 });
    }

    private sealed class ResourceOnlySkill : AgentClassSkill
    {
        public override AgentSkillFrontmatter Frontmatter { get; } = new("resource-only", "Skill with resources only.");

        public override string Instructions => "Body.";

        public override IReadOnlyList<AgentSkillResource>? Resources { get; } =
        [
            new AgentCodeSkillResource("data", "some data"),
        ];
    }

    private sealed class ScriptOnlySkill : AgentClassSkill
    {
        public override AgentSkillFrontmatter Frontmatter { get; } = new("script-only", "Skill with scripts only.");

        public override string Instructions => "Body.";

        public override IReadOnlyList<AgentSkillScript>? Scripts { get; } =
        [
            new AgentCodeSkillScript((string input) => input.ToUpperInvariant(), "ToUpper"),
        ];
    }

    private sealed class InvalidNameClassSkill : AgentClassSkill
    {
        public override AgentSkillFrontmatter Frontmatter { get; } = new("INVALID-NAME", "An invalid skill.");

        public override string Instructions => "Body.";
    }

    #endregion
}
