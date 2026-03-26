// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace Microsoft.Agents.AI;

/// <summary>
/// Internal helper that builds XML-structured content strings for code-defined and class-based skills.
/// </summary>
internal static class SkillContentBuilder
{
    /// <summary>
    /// Builds the skill body containing instructions, resources, and scripts as XML elements.
    /// </summary>
    /// <param name="instructions">The raw instructions text.</param>
    /// <param name="resources">Optional resources associated with the skill.</param>
    /// <param name="scripts">Optional scripts associated with the skill.</param>
    /// <returns>An XML-structured body string.</returns>
    public static string BuildBody(
        string instructions,
        IReadOnlyList<AgentSkillResource>? resources,
        IReadOnlyList<AgentSkillScript>? scripts)
    {
        var sb = new StringBuilder();

        sb.Append("<instructions>\n");
        sb.Append(instructions);
        sb.Append("\n</instructions>");

        if (resources is { Count: > 0 })
        {
            sb.Append("\n\n<resources>\n");
            foreach (var resource in resources)
            {
                if (resource.Description is not null)
                {
                    sb.Append($"  <resource name=\"{XmlEscape(resource.Name)}\" description=\"{XmlEscape(resource.Description)}\"/>\n");
                }
                else
                {
                    sb.Append($"  <resource name=\"{XmlEscape(resource.Name)}\"/>\n");
                }
            }

            sb.Append("</resources>");
        }

        if (scripts is { Count: > 0 })
        {
            sb.Append("\n\n<scripts>\n");
            foreach (var script in scripts)
            {
                JsonElement? parametersSchema = (script as AgentInlineSkillScript)?.ParametersSchema;

                if (script.Description is null && parametersSchema is null)
                {
                    sb.Append($"  <script name=\"{XmlEscape(script.Name)}\"/>\n");
                }
                else
                {
                    sb.Append(script.Description is not null
                        ? $"  <script name=\"{XmlEscape(script.Name)}\" description=\"{XmlEscape(script.Description)}\">\n"
                        : $"  <script name=\"{XmlEscape(script.Name)}\">\n");

                    if (parametersSchema is not null)
                    {
                        sb.Append($"    <parameters_schema>{parametersSchema.Value.GetRawText()}</parameters_schema>\n");
                    }

                    sb.Append("  </script>\n");
                }
            }

            sb.Append("</scripts>");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Builds the full skill content containing name, description, and body as XML elements.
    /// </summary>
    /// <param name="name">The skill name.</param>
    /// <param name="description">The skill description.</param>
    /// <param name="body">The pre-built body (from <see cref="BuildBody"/>).</param>
    /// <returns>An XML-structured content string.</returns>
    public static string BuildContent(string name, string description, string body)
    {
        return $"<name>{XmlEscape(name)}</name>\n<description>{XmlEscape(description)}</description>\n\n{body}";
    }

    private static string XmlEscape(string value)
    {
        return value
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;");
    }
}
