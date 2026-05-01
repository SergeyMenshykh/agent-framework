// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using A2A;
using Microsoft.Extensions.AI;

namespace Microsoft.Agents.AI.A2A.UnitTests;

/// <summary>
/// Unit tests for the <see cref="AgentTaskStatusExtensions"/> class.
/// </summary>
public sealed class AgentTaskStatusExtensionsTests
{
    [Fact]
    public void GetUserInputRequests_WithNullMessage_ReturnsNull()
    {
        // Arrange
        var status = new AgentTaskStatus
        {
            State = TaskState.InputRequired,
            Message = null,
        };

        // Act
        IList<AIContent>? result = status.GetUserInputRequests();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetUserInputRequests_WithNotInputRequiredState_ReturnsNull()
    {
        // Arrange
        var status = new AgentTaskStatus
        {
            State = TaskState.Completed,
            Message = new AgentMessage { Parts = [new TextPart { Text = "Some text" }] },
        };

        // Act
        IList<AIContent>? result = status.GetUserInputRequests();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetUserInputRequests_WithInputRequiredStateAndMultipleRequests_ReturnsAIContentList()
    {
        // Arrange
        var status = new AgentTaskStatus
        {
            State = TaskState.InputRequired,
            Message = new AgentMessage
            {
                Parts =
                [
                    new TextPart { Text = "First request" },
                    new TextPart { Text = "Second request" },
                    new TextPart { Text = "Third request" }
                ],
            },
        };

        // Act
        IList<AIContent>? result = status.GetUserInputRequests();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        Assert.Equal("First request", ((TextInputRequestContent)result[0]).Request);
        Assert.Equal("Second request", ((TextInputRequestContent)result[1]).Request);
        Assert.Equal("Third request", ((TextInputRequestContent)result[2]).Request);
    }

    [Fact]
    public void GetUserInputRequests_WithTextParts_SetsRawRepresentationAndAdditionalPropertiesCorrectly()
    {
        // Arrange
        var textPart = new TextPart
        {
            Text = "Input request",
            Metadata = new Dictionary<string, System.Text.Json.JsonElement>
            {
                { "key1", System.Text.Json.JsonSerializer.SerializeToElement("value1") },
                { "key2", System.Text.Json.JsonSerializer.SerializeToElement("value2") }
            }
        };
        var status = new AgentTaskStatus
        {
            State = TaskState.InputRequired,
            Message = new AgentMessage { Parts = [textPart] },
        };

        // Act
        IList<AIContent>? result = status.GetUserInputRequests();

        // Assert
        Assert.NotNull(result);
        var content = Assert.IsType<TextInputRequestContent>(result[0]);
        Assert.Equal(textPart, content.RawRepresentation);
        Assert.NotNull(content.AdditionalProperties);
        Assert.True(content.AdditionalProperties.ContainsKey("key1"));
        Assert.True(content.AdditionalProperties.ContainsKey("key2"));
    }

    [Fact]
    public void GetUserInputRequests_WithEmptyMessageParts_ReturnsNull()
    {
        // Arrange
        var status = new AgentTaskStatus
        {
            State = TaskState.InputRequired,
            Message = new AgentMessage { Parts = [] },
        };

        // Act
        IList<AIContent>? result = status.GetUserInputRequests();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetUserInputRequests_GeneratesUniqueIds()
    {
        // Arrange
        var status = new AgentTaskStatus
        {
            State = TaskState.InputRequired,
            Message = new AgentMessage
            {
                Parts =
                [
                    new TextPart { Text = "First" },
                    new TextPart { Text = "Second" },
                ],
            },
        };

        // Act
        IList<AIContent>? result = status.GetUserInputRequests();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        var id1 = ((TextInputRequestContent)result[0]).Id;
        var id2 = ((TextInputRequestContent)result[1]).Id;
        Assert.NotEqual(id1, id2);
    }
}
