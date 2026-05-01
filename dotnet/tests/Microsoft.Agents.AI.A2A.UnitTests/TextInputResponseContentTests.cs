// Copyright (c) Microsoft. All rights reserved.

using System;

namespace Microsoft.Agents.AI.A2A.UnitTests;

/// <summary>
/// Unit tests for the <see cref="TextInputResponseContent"/> class.
/// </summary>
public sealed class TextInputResponseContentTests
{
    [Fact]
    public void Constructor_WithValidParameters_InitializesPropertiesCorrectly()
    {
        // Arrange
        const string Id = "response-456";
        const string Response = "User's answer";

        // Act
        var content = new TextInputResponseContent(Id, Response);

        // Assert
        Assert.Equal(Id, content.Id);
        Assert.Equal(Response, content.Response);
    }

    [Fact]
    public void Constructor_WithNullId_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TextInputResponseContent(null!, "This is my response"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidId_ThrowsArgumentException(string invalidId)
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentException>(() => new TextInputResponseContent(invalidId, "This is my response"));
    }

    [Fact]
    public void Constructor_WithNullResponse_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TextInputResponseContent("test-response-123", null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidResponse_ThrowsArgumentException(string invalidResponse)
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentException>(() => new TextInputResponseContent("test-response-123", invalidResponse));
    }
}
