// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using A2A;
using Microsoft.Extensions.AI;

namespace Microsoft.Agents.AI.A2A.UnitTests;

/// <summary>
/// Unit tests for the <see cref="A2AAIContentExtensions"/> class.
/// </summary>
public sealed class A2AAIContentExtensionsTests
{
    [Fact]
    public void ToA2AParts_WithEmptyCollection_ReturnsNull()
    {
        // Arrange
        var emptyContents = new List<AIContent>();

        // Act
        var result = emptyContents.ToParts();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ToA2AParts_WithMultipleContents_ReturnsListWithAllParts()
    {
        // Arrange
        var contents = new List<AIContent>
        {
            new TextContent("First text"),
            new UriContent("https://example.com/file1.txt", "file/txt"),
            new TextContent("Second text"),
        };

        // Act
        var result = contents.ToParts();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);

        Assert.Equal(PartContentCase.Text, result[0].ContentCase);
        Assert.Equal("First text", result[0].Text);

        Assert.Equal(PartContentCase.Url, result[1].ContentCase);
        Assert.Equal("https://example.com/file1.txt", result[1].Url);

        Assert.Equal(PartContentCase.Text, result[2].ContentCase);
        Assert.Equal("Second text", result[2].Text);
    }

    [Fact]
    public void ToA2AParts_WithMixedSupportedAndUnsupportedContent_IgnoresUnsupportedContent()
    {
        // Arrange
        var contents = new List<AIContent>
        {
            new TextContent("First text"),
            new MockAIContent(), // Unsupported - should be ignored
            new UriContent("https://example.com/file.txt", "file/txt"),
            new MockAIContent(), // Unsupported - should be ignored
            new TextContent("Second text")
        };

        // Act
        var result = contents.ToParts();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);

        Assert.Equal(PartContentCase.Text, result[0].ContentCase);
        Assert.Equal("First text", result[0].Text);

        Assert.Equal(PartContentCase.Url, result[1].ContentCase);
        Assert.Equal("https://example.com/file.txt", result[1].Url);

        Assert.Equal(PartContentCase.Text, result[2].ContentCase);
        Assert.Equal("Second text", result[2].Text);
    }

    [Fact]
    public void ToA2AParts_WithTextInputResponseContent_ReturnsTextPartWithResponse()
    {
        // Arrange
        var contents = new List<AIContent>
        {
            new TextInputResponseContent("req-1", "User input response")
        };

        // Act
        var result = contents.ToParts();

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);

        var textPart = Assert.IsType<TextPart>(result[0]);
        Assert.Equal("User input response", textPart.Text);
    }

    [Fact]
    public void ToA2AParts_WithTextContentAndTextInputResponseContent_ReturnsMultipleParts()
    {
        // Arrange
        var contents = new List<AIContent>
        {
            new TextContent("Regular text"),
            new TextInputResponseContent("req-1", "User input response")
        };

        // Act
        var result = contents.ToParts();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);

        var firstTextPart = Assert.IsType<TextPart>(result[0]);
        Assert.Equal("Regular text", firstTextPart.Text);

        var secondTextPart = Assert.IsType<TextPart>(result[1]);
        Assert.Equal("User input response", secondTextPart.Text);
    }

    [Fact]
    public void ToA2AParts_WithMultipleTextInputResponseContents_ReturnsMultipleTextParts()
    {
        // Arrange
        var contents = new List<AIContent>
        {
            new TextInputResponseContent("req-1", "First response"),
            new TextInputResponseContent("req-2", "Second response"),
            new TextInputResponseContent("req-3", "Third response")
        };

        // Act
        var result = contents.ToParts();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);

        var firstPart = Assert.IsType<TextPart>(result[0]);
        Assert.Equal("First response", firstPart.Text);

        var secondPart = Assert.IsType<TextPart>(result[1]);
        Assert.Equal("Second response", secondPart.Text);

        var thirdPart = Assert.IsType<TextPart>(result[2]);
        Assert.Equal("Third response", thirdPart.Text);
    }

    [Fact]
    public void ToA2AParts_WithMixedContentAndTextInputResponseContent_ReturnsCorrectOrder()
    {
        // Arrange
        var contents = new List<AIContent>
        {
            new TextContent("Start"),
            new TextInputResponseContent("req-1", "Response"),
            new UriContent("https://example.com/file.txt", "file/txt"),
            new TextInputResponseContent("req-2", "Another response")
        };

        // Act
        var result = contents.ToParts();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(4, result.Count);

        var firstPart = Assert.IsType<TextPart>(result[0]);
        Assert.Equal("Start", firstPart.Text);

        var secondPart = Assert.IsType<TextPart>(result[1]);
        Assert.Equal("Response", secondPart.Text);

        var thirdPart = Assert.IsType<FilePart>(result[2]);
        Assert.Equal("https://example.com/file.txt", thirdPart.File.Uri?.ToString());

        var fourthPart = Assert.IsType<TextPart>(result[3]);
        Assert.Equal("Another response", fourthPart.Text);
    }

    // Mock class for testing unsupported scenarios
    private sealed class MockAIContent : AIContent;
}
