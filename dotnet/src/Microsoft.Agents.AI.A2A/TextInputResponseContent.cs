// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.AI;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Agents.AI.A2A;

/// <summary>
/// Represents the text response to a text request for user input.
/// </summary>
public class TextInputResponseContent : InputResponseContent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TextInputResponseContent"/> class.
    /// </summary>
    /// <param name="id">The ID that uniquely identifies the user input request/response pair.</param>
    /// <param name="text">The text response to the user text request.</param>
    public TextInputResponseContent(string id, string text) : base(id)
    {
        this.Response = Throw.IfNullOrWhitespace(text);
    }

    /// <summary>
    /// The text response to the user text request.
    /// </summary>
    public string Response { get; }
}
