// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.AI;

namespace Microsoft.Agents.AI.UnitTests.AgentSkills;

/// <summary>
/// Unit tests for <see cref="HostedCodeInterpreterSkillScriptExecutor"/>.
/// </summary>
public sealed class HostedCodeInterpreterSkillScriptExecutorTests
{
    [Fact]
    public void GetInstructions_ReturnsScriptExecutionGuidance()
    {
        // Arrange
        var executor = new HostedCodeInterpreterSkillScriptExecutor();

        // Act
        string? instructions = executor.Instructions;

        // Assert
        Assert.NotNull(instructions);
        Assert.Contains("read_skill_resource", instructions);
        Assert.Contains("code interpreter", instructions);
    }

    [Fact]
    public void GetTools_ReturnsSingleHostedCodeInterpreterTool()
    {
        // Arrange
        var executor = new HostedCodeInterpreterSkillScriptExecutor();

        // Act
        var tools = executor.Tools;

        // Assert
        Assert.NotNull(tools);
        Assert.Single(tools!);
        Assert.IsType<HostedCodeInterpreterTool>(tools![0]);
    }

    [Fact]
    public void GetTools_ReturnsSameInstanceOnMultipleCalls()
    {
        // Arrange
        var executor = new HostedCodeInterpreterSkillScriptExecutor();

        // Act
        var tools1 = executor.Tools;
        var tools2 = executor.Tools;

        // Assert — static tools array should be reused
        Assert.Same(tools1, tools2);
    }

    [Fact]
    public void FactoryMethod_ReturnsHostedCodeInterpreterSkillScriptExecutor()
    {
        // Act
        var executor = SkillScriptExecutor.HostedCodeInterpreter();

        // Assert
        Assert.IsType<HostedCodeInterpreterSkillScriptExecutor>(executor);
    }
}
