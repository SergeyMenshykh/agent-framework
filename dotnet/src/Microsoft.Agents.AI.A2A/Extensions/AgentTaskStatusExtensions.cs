// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using Microsoft.Agents.AI.A2A;
using Microsoft.Extensions.AI;
using Microsoft.Shared.Diagnostics;

namespace A2A;

/// <summary>
/// Extension methods for the <see cref="AgentTaskStatus"/> class.
/// </summary>
internal static class AgentTaskStatusExtensions
{
    internal static IList<AIContent>? GetUserInputRequests(this AgentTaskStatus status)
    {
        _ = Throw.IfNull(status);

        List<AIContent>? contents = null;

        if (status.Message is null || status.State is not TaskState.InputRequired)
        {
            return contents;
        }

        foreach (var part in status.Message.Parts)
        {
            if (part is TextPart textPart)
            {
                (contents ??= []).Add(new TextInputRequestContent(Guid.NewGuid().ToString(), textPart.Text)
                {
                    RawRepresentation = part,
                    AdditionalProperties = part.Metadata.ToAdditionalProperties(),
                });
            }
        }

        return contents;
    }
}
