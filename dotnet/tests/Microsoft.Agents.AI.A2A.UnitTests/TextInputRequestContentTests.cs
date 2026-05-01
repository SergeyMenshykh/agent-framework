// Copyright (c) Microsoft. All rights reserved.

using System;

namespace Microsoft.Agents.AI.A2A.UnitTests;

/// <summary>
/// Unit tests for the <see cref="TextInputRequestContent"/> class.
/// </summary>
public sealed class TextInputRequestContentTests
{
    [Fact]
    public void Constructor_WithValidParameters_InitializesPropertiesCorrectly()
    {
        // Arrange
        const string Id = "input-456";
        const string Request = "What is your name?";

        // Act
        var content = new TextInputRequestContent(Id, Request);

        // Assert
        Assert.Equal(Id, content.Id);
        Assert.Equal(Request, content.Request);
    }

    [Fact]
    public void Constructor_WithNullId_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TextInputRequestContent(null!, "Please provide your feedback"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidId_ThrowsArgumentException(string invalidId)
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentException>(() => new TextInputRequestContent(invalidId, "Please provide your feedback"));
    }

    [Fact]
    public void Constructor_WithNullRequest_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TextInputRequestContent("test-input-123", null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidRequest_ThrowsArgumentException(string invalidRequest)
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentException>(() => new TextInputRequestContent("test-input-123", invalidRequest));
    }

    [Fact]
    public void CreateResponse_WithValidText_PreservesIdAndIncludesResponse()
    {
        // Arrange
        const string Id = "input-101";
        var content = new TextInputRequestContent(Id, "Please provide your feedback");
        const string ResponseText = "My response";

        // Act
        var response = content.CreateResponse(ResponseText);

        // Assert
        Assert.Equal(Id, response.Id);
        Assert.Equal(ResponseText, response.Response);
    }

    [Fact]
    public void CreateResponse_WithNullText_ThrowsArgumentNullException()
    {
        // Arrange
        var content = new TextInputRequestContent("test-input-123", "Please provide your feedback");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => content.CreateResponse(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateResponse_WithInvalidResponse_ThrowsArgumentException(string invalidText)
    {
        // Arrange
        var content = new TextInputRequestContent("test-input-123", "Please provide your feedback");

        // Act & Assert
        Assert.Throws<ArgumentException>(() => content.CreateResponse(invalidText));
    }
}
