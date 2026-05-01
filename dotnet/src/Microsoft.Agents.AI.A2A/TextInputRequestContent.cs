// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.AI;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Agents.AI.A2A;

/// <summary>
/// Represents the text request for user input.
/// </summary>
public sealed class TextInputRequestContent : InputRequestContent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TextInputRequestContent"/> class.
    /// </summary>
    /// <param name="id">The ID that uniquely identifies the user input request/response pair.</param>
    /// <param name="request">The text request for user input.</param>
    public TextInputRequestContent(string id, string request) : base(id)
    {
        this.Request = Throw.IfNullOrWhitespace(request);
    }

    /// <summary>
    /// The text request for user input.
    /// </summary>
    public string Request { get; }

    /// <summary>
    /// Creates a <see cref="TextInputResponseContent"/> to provide the user's response to this request.
    /// </summary>
    /// <param name="response">The text response provided by the user.</param>
    /// <returns>The <see cref="TextInputResponseContent"/> representing the approval response.</returns>
    public TextInputResponseContent CreateResponse(string response) => new(this.RequestId, response);
}
