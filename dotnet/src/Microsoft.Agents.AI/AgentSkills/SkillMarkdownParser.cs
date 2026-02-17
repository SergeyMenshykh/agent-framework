// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Shared.Diagnostics;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Microsoft.Agents.AI;

/// <summary>
/// Parses SKILL.md files to extract skill metadata and content.
/// </summary>
/// <remarks>
/// SKILL.md files follow a specific format with YAML frontmatter for metadata
/// and markdown body for instructions. The parser extracts both components and
/// creates an <see cref="AIAgentSkill"/> object.
/// </remarks>
internal static class SkillMarkdownParser
{
    private static readonly Regex s_frontmatterRegex = new(@"^---\s*$(.+?)^---\s*$", RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.Compiled);

#pragma warning disable IL3050 // AOT analysis: Using reflection-based deserializer (static alternative requires code generation)
    private static readonly IDeserializer s_yamlDeserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();
#pragma warning restore IL3050

    /// <summary>
    /// Parses a SKILL.md file and returns an <see cref="AIAgentSkill"/> object.
    /// </summary>
    /// <param name="skillFilePath">The full path to the SKILL.md file.</param>
    /// <param name="skillDirectoryPath">The directory containing the skill.</param>
    /// <returns>An <see cref="AIAgentSkill"/> object populated from the file.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="skillFilePath"/> or <paramref name="skillDirectoryPath"/> is <see langword="null"/>.</exception>
    /// <exception cref="FileNotFoundException">The skill file does not exist.</exception>
    /// <exception cref="InvalidOperationException">The SKILL.md file is malformed or missing required fields.</exception>
    public static AIAgentSkill Parse(string skillFilePath, string skillDirectoryPath)
    {
        _ = Throw.IfNullOrWhitespace(skillFilePath);
        _ = Throw.IfNullOrWhitespace(skillDirectoryPath);

        if (!File.Exists(skillFilePath))
        {
            throw new FileNotFoundException($"Skill file not found: {skillFilePath}");
        }

        string fileContent = File.ReadAllText(skillFilePath, Encoding.UTF8);
        return ParseContent(fileContent, skillDirectoryPath);
    }

    /// <summary>
    /// Parses SKILL.md content and returns an <see cref="AIAgentSkill"/> object.
    /// </summary>
    /// <param name="content">The content of the SKILL.md file.</param>
    /// <param name="skillDirectoryPath">The directory containing the skill.</param>
    /// <returns>An <see cref="AIAgentSkill"/> object populated from the content.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="content"/> or <paramref name="skillDirectoryPath"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">The content is malformed or missing required fields.</exception>
    internal static AIAgentSkill ParseContent(string content, string skillDirectoryPath)
    {
        _ = Throw.IfNullOrWhitespace(content);
        _ = Throw.IfNullOrWhitespace(skillDirectoryPath);

        // Extract frontmatter and body
        Match match = s_frontmatterRegex.Match(content);
        if (!match.Success)
        {
            throw new InvalidOperationException("SKILL.md file must contain YAML frontmatter delimited by '---'.");
        }

        string yamlContent = match.Groups[1].Value.Trim();
        string markdownBody = content.Substring(match.Index + match.Length).Trim();

        if (string.IsNullOrWhiteSpace(markdownBody))
        {
            throw new InvalidOperationException("SKILL.md file must contain markdown body with skill instructions.");
        }

        // Parse YAML frontmatter
        SkillMetadata? metadata;
        try
        {
            metadata = s_yamlDeserializer.Deserialize<SkillMetadata>(yamlContent);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to parse YAML frontmatter in SKILL.md.", ex);
        }

        if (metadata is null)
        {
            throw new InvalidOperationException("YAML frontmatter in SKILL.md is empty or invalid.");
        }

        // Validate required fields
        if (string.IsNullOrWhiteSpace(metadata.Name))
        {
            throw new InvalidOperationException("SKILL.md frontmatter must contain a 'name' field.");
        }

        if (string.IsNullOrWhiteSpace(metadata.Description))
        {
            throw new InvalidOperationException("SKILL.md frontmatter must contain a 'description' field.");
        }

        // Discover resources from references/ directory
        List<AgentSkillResource> resources = [];
        string referencesDir = Path.Combine(skillDirectoryPath, "references");
        if (Directory.Exists(referencesDir))
        {
            foreach (string resourcePath in Directory.GetFiles(referencesDir, "*.*", SearchOption.TopDirectoryOnly))
            {
                string resourceName = Path.Combine("references", Path.GetFileName(resourcePath));
                string resourceContent = File.ReadAllText(resourcePath, Encoding.UTF8);
                resources.Add(new AgentSkillResource(resourceName, resourceContent));
            }
        }

        // Discover scripts from scripts/ directory
        List<AgentSkillScript> scripts = [];
        string scriptsDir = Path.Combine(skillDirectoryPath, "scripts");
        if (Directory.Exists(scriptsDir))
        {
            foreach (string scriptPath in Directory.GetFiles(scriptsDir, "*.*", SearchOption.TopDirectoryOnly))
            {
                string scriptName = Path.GetFileNameWithoutExtension(scriptPath);
                scripts.Add(new AgentSkillScript(scriptName, scriptPath));
            }
        }

        // Convert metadata to dictionary (merge Metadata field with AdditionalData if both exist)
        IReadOnlyDictionary<string, string>? additionalMetadata = null;
        if (metadata.Metadata?.Count > 0 || metadata.AdditionalData?.Count > 0)
        {
            var combined = new Dictionary<string, string>();
            if (metadata.Metadata is not null)
            {
                foreach (var kvp in metadata.Metadata)
                {
                    combined[kvp.Key] = kvp.Value;
                }
            }
            if (metadata.AdditionalData is not null)
            {
                foreach (var kvp in metadata.AdditionalData)
                {
                    combined[kvp.Key] = kvp.Value;
                }
            }
            additionalMetadata = combined;
        }

        return new AIAgentSkill(
            name: metadata.Name,
            description: metadata.Description,
            content: markdownBody,
            resources: resources,
            scripts: scripts,
            compatibility: metadata.Compatibility,
            metadata: additionalMetadata,
            sourcePath: skillDirectoryPath);
    }

    /// <summary>
    /// Represents the YAML frontmatter structure in SKILL.md files.
    /// </summary>
    private sealed class SkillMetadata
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? License { get; set; }
        public string? Compatibility { get; set; }
        public Dictionary<string, string>? Metadata { get; set; }

        // Capture any additional fields not explicitly mapped
        [YamlMember(Alias = "*", ApplyNamingConventions = false)]
        public Dictionary<string, string>? AdditionalData { get; set; }
    }
}
