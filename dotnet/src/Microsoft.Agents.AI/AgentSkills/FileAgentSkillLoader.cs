// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.Agents.AI;

/// <summary>
/// Discovers, parses, and validates SKILL.md files from filesystem directories.
/// </summary>
/// <remarks>
/// Searches directories recursively (up to <see cref="MaxSearchDepth"/> levels) for SKILL.md files.
/// Each file is validated for YAML frontmatter and resource integrity. Invalid skills are excluded
/// with logged warnings. Resource paths are checked against path traversal and symlink escape attacks.
/// </remarks>
internal sealed partial class FileAgentSkillLoader
{
    private const string SkillFileName = "SKILL.md";
    private const int MaxSearchDepth = 2;
    private const int MaxNameLength = 64;
    private const int MaxDescriptionLength = 1024;

    // Matches YAML frontmatter delimited by "---" lines. Group 1 = content between delimiters.
    // Multiline makes ^/$ match line boundaries; Singleline makes . match newlines across the block.
    // Example: "---\nname: foo\n---\nBody" → Group 1: "name: foo\n"
#if NET
    private static readonly Regex s_frontmatterRegex = new(@"^---\s*$(.+?)^---\s*$", RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.Compiled, TimeSpan.FromSeconds(5));
#else
    private static readonly Regex s_frontmatterRegex = new(@"^---\s*$(.+?)^---\s*$", RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.Compiled);
#endif

    // Matches markdown links to local resource files. Group 1 = relative file path.
    // Supports optional ./ or ../ prefixes; excludes URLs (no ":" in the path character class).
    // Examples: [doc](refs/FAQ.md) → "refs/FAQ.md", [s](./s.json) → "./s.json",
    //           [p](../shared/doc.txt) → "../shared/doc.txt"
#if NET
    private static readonly Regex s_resourceLinkRegex = new(@"\[.*?\]\((\.?\.?/?[\w][\w\-./]*\.\w+)\)", RegexOptions.Compiled, TimeSpan.FromSeconds(5));
#else
    private static readonly Regex s_resourceLinkRegex = new(@"\[.*?\]\((\.?\.?/?[\w][\w\-./]*\.\w+)\)", RegexOptions.Compiled);
#endif

    // Matches YAML "key: value" lines. Group 1 = key, Group 2 = quoted value, Group 3 = unquoted value.
    // Accepts single or double quotes; the lazy quantifier trims trailing whitespace on unquoted values.
    // Examples: "name: foo" → (name, _, foo), "name: 'foo bar'" → (name, foo bar, _),
    //           "description: \"A skill\"" → (description, A skill, _)
#if NET
    private static readonly Regex s_yamlKeyValueRegex = new(@"^\s*(\w+)\s*:\s*(?:[""'](.+?)[""']|(.+?))\s*$", RegexOptions.Multiline | RegexOptions.Compiled, TimeSpan.FromSeconds(5));
#else
    private static readonly Regex s_yamlKeyValueRegex = new(@"^\s*(\w+)\s*:\s*(?:[""'](.+?)[""']|(.+?))\s*$", RegexOptions.Multiline | RegexOptions.Compiled);
#endif

    // Validates skill names: lowercase letters, numbers, and hyphens only; must not start or end with a hyphen.
    // Examples: "my-skill" ✓, "skill123" ✓, "-bad" ✗, "bad-" ✗, "Bad" ✗
    private static readonly Regex s_validNameRegex = new(@"^[a-z0-9]([a-z0-9\-]*[a-z0-9])?$", RegexOptions.Compiled);

    private readonly ILogger _logger;
    private readonly HashSet<string>? _allowedResourceExtensions;
    private readonly List<string>? _allowedResourcePaths;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileAgentSkillLoader"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="options">Optional configuration options.</param>
    internal FileAgentSkillLoader(ILogger logger, FileAgentSkillsProviderOptions? options = null)
    {
        this._logger = logger;
        this._allowedResourceExtensions = options?.AllowedResourceExtensions != null
            ? new HashSet<string>(options.AllowedResourceExtensions, StringComparer.OrdinalIgnoreCase)
            : null;
        this._allowedResourcePaths = options?.AllowedResourcePaths != null
            ? options.AllowedResourcePaths
                .Select(p => p.TrimEnd('/'))
                .Where(p => p.Length > 0)
                .ToList()
            : null;
    }

    /// <summary>
    /// Discovers skill directories and loads valid skills from them.
    /// </summary>
    /// <param name="skillPaths">Paths to search for skills. Each path can point to an individual skill folder or a parent folder.</param>
    /// <returns>A dictionary of loaded skills keyed by skill name.</returns>
    internal Dictionary<string, AIAgentSkill> DiscoverAndLoadSkills(IEnumerable<string> skillPaths)
    {
        var skills = new Dictionary<string, AIAgentSkill>(StringComparer.OrdinalIgnoreCase);

        var discoveredPaths = DiscoverSkillDirectories(skillPaths);

        LogSkillsDiscovered(this._logger, discoveredPaths.Count);

        foreach (string skillPath in discoveredPaths)
        {
            AIAgentSkill? skill = this.ParseSkillFile(skillPath);
            if (skill is null)
            {
                continue;
            }

            if (skills.TryGetValue(skill.Frontmatter.Name, out AIAgentSkill? existing))
            {
                LogDuplicateSkillName(this._logger, skill.Frontmatter.Name, existing.SourcePath, skillPath);
            }

            skills[skill.Frontmatter.Name] = skill;

            LogSkillLoaded(this._logger, skill.Frontmatter.Name);
        }

        LogSkillsLoadedTotal(this._logger, skills.Count);

        return skills;
    }

    private static List<string> DiscoverSkillDirectories(IEnumerable<string> skillPaths)
    {
        var discoveredPaths = new List<string>();

        foreach (string rootDirectory in skillPaths)
        {
            if (string.IsNullOrWhiteSpace(rootDirectory) || !Directory.Exists(rootDirectory))
            {
                continue;
            }

            SearchDirectoriesForSkills(rootDirectory, discoveredPaths, currentDepth: 0);
        }

        return discoveredPaths;
    }

    private static void SearchDirectoriesForSkills(string directory, List<string> results, int currentDepth)
    {
        string skillFilePath = Path.Combine(directory, SkillFileName);
        if (File.Exists(skillFilePath))
        {
            results.Add(directory);
        }

        if (currentDepth >= MaxSearchDepth)
        {
            return;
        }

        foreach (string subdirectory in Directory.GetDirectories(directory))
        {
            SearchDirectoriesForSkills(subdirectory, results, currentDepth + 1);
        }
    }

    private AIAgentSkill? ParseSkillFile(string skillDirectoryPath)
    {
        string skillFilePath = Path.Combine(skillDirectoryPath, SkillFileName);

        string content = File.ReadAllText(skillFilePath, Encoding.UTF8);

        if (!this.TryParseSkillDocument(content, skillFilePath, out SkillFrontmatter frontmatter, out string body))
        {
            return null;
        }

        List<string> resourceNames = ExtractResourcePaths(body);

        if (!this.ValidateResources(skillDirectoryPath, resourceNames, frontmatter.Name))
        {
            return null;
        }

        return new AIAgentSkill(
            frontmatter: frontmatter,
            body: body,
            sourcePath: skillDirectoryPath,
            resourceNames: resourceNames);
    }

    private bool TryParseSkillDocument(string content, string skillFilePath, out SkillFrontmatter frontmatter, out string body)
    {
        frontmatter = null!;
        body = null!;

        Match match = s_frontmatterRegex.Match(content);
        if (!match.Success)
        {
            LogInvalidFrontmatter(this._logger, skillFilePath);
            return false;
        }

        string? name = null;
        string? description = null;

        string yamlContent = match.Groups[1].Value.Trim();

        foreach (Match kvMatch in s_yamlKeyValueRegex.Matches(yamlContent))
        {
            string key = kvMatch.Groups[1].Value;
            string value = kvMatch.Groups[2].Success ? kvMatch.Groups[2].Value : kvMatch.Groups[3].Value;

            if (string.Equals(key, "name", StringComparison.OrdinalIgnoreCase))
            {
                name = value;
            }
            else if (string.Equals(key, "description", StringComparison.OrdinalIgnoreCase))
            {
                description = value;
            }
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            LogMissingFrontmatterField(this._logger, skillFilePath, "name");
            return false;
        }

        if (name.Length > MaxNameLength || !s_validNameRegex.IsMatch(name))
        {
            LogInvalidFieldValue(this._logger, skillFilePath, "name", $"Must be {MaxNameLength} characters or fewer, using only lowercase letters, numbers, and hyphens, and must not start or end with a hyphen.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            LogMissingFrontmatterField(this._logger, skillFilePath, "description");
            return false;
        }

        if (description.Length > MaxDescriptionLength)
        {
            LogInvalidFieldValue(this._logger, skillFilePath, "description", $"Must be {MaxDescriptionLength} characters or fewer.");
            return false;
        }

        frontmatter = new SkillFrontmatter(name, description);
        body = content.Substring(match.Index + match.Length).TrimStart();

        return true;
    }

    private bool ValidateResources(string skillDirectoryPath, List<string> resourceNames, string skillName)
    {
        string normalizedSkillPath = Path.GetFullPath(skillDirectoryPath) + Path.DirectorySeparatorChar;

        foreach (string resourceName in resourceNames)
        {
            if (!this.IsResourceExtensionAllowed(resourceName, skillName) ||
                !this.IsResourcePathAllowed(resourceName, skillName))
            {
                return false;
            }

            string fullPath = Path.GetFullPath(Path.Combine(skillDirectoryPath, resourceName));

            if (!IsPathWithinDirectory(fullPath, normalizedSkillPath))
            {
                LogResourcePathTraversal(this._logger, skillName, resourceName);
                return false;
            }

#if NET
            if (!IsSymlinkWithinDirectory(fullPath, normalizedSkillPath))
            {
                LogResourceSymlinkEscape(this._logger, skillName, resourceName);
                return false;
            }
#endif

            if (!File.Exists(fullPath))
            {
                LogMissingResource(this._logger, skillName, resourceName, fullPath);
                return false;
            }
        }

        return true;
    }

    private bool IsResourceExtensionAllowed(string resourceName, string skillName)
    {
        if (this._allowedResourceExtensions == null)
        {
            return true;
        }

        string extension = Path.GetExtension(resourceName);

        if (!this._allowedResourceExtensions.Contains(extension))
        {
            LogDisallowedResourceExtension(this._logger, skillName, resourceName, extension);
            return false;
        }

        return true;
    }

    private bool IsResourcePathAllowed(string resourceName, string skillName)
    {
        if (this._allowedResourcePaths == null)
        {
            return true;
        }

        bool matched = this._allowedResourcePaths.Exists(prefix =>
            resourceName.StartsWith(prefix + "/", StringComparison.OrdinalIgnoreCase) ||
            resourceName.Equals(prefix, StringComparison.OrdinalIgnoreCase));

        if (!matched)
        {
            LogDisallowedResourcePath(this._logger, skillName, resourceName);
        }

        return matched;
    }

    /// <summary>
    /// Checks that <paramref name="fullPath"/> is under <paramref name="normalizedDirectoryPath"/>,
    /// guarding against path traversal attacks.
    /// </summary>
    private static bool IsPathWithinDirectory(string fullPath, string normalizedDirectoryPath)
    {
        return fullPath.StartsWith(normalizedDirectoryPath, StringComparison.OrdinalIgnoreCase);
    }

#if NET
    /// <summary>
    /// Checks that a symlink at <paramref name="fullPath"/> does not resolve outside
    /// <paramref name="normalizedDirectoryPath"/>.
    /// </summary>
    private static bool IsSymlinkWithinDirectory(string fullPath, string normalizedDirectoryPath)
    {
        var fileInfo = new FileInfo(fullPath);
        if (fileInfo.LinkTarget != null)
        {
            string resolvedTarget = Path.GetFullPath(fileInfo.LinkTarget, Path.GetDirectoryName(fullPath)!);
            return resolvedTarget.StartsWith(normalizedDirectoryPath, StringComparison.OrdinalIgnoreCase);
        }

        return true;
    }
#endif

    private static List<string> ExtractResourcePaths(string content)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var paths = new List<string>();
        foreach (Match m in s_resourceLinkRegex.Matches(content))
        {
            string path = m.Groups[1].Value;
            if (seen.Add(path))
            {
                paths.Add(path);
            }
        }

        return paths;
    }

    /// <summary>
    /// Reads a resource file from disk with path traversal and symlink guards.
    /// </summary>
    /// <param name="skill">The skill that owns the resource.</param>
    /// <param name="resourceName">Relative path of the resource within the skill directory.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The UTF-8 text content of the resource file.</returns>
    /// <exception cref="InvalidOperationException">
    /// The resource is not registered, resolves outside the skill directory, or does not exist.
    /// </exception>
    internal async Task<string> ReadSkillResourceAsync(AIAgentSkill skill, string resourceName, CancellationToken cancellationToken = default)
    {
        if (!skill.ResourceNames.Any(r => r.Equals(resourceName, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException($"Resource '{resourceName}' not found in skill '{skill.Frontmatter.Name}'.");
        }

        string fullPath = Path.GetFullPath(Path.Combine(skill.SourcePath, resourceName));
        string normalizedSourcePath = Path.GetFullPath(skill.SourcePath) + Path.DirectorySeparatorChar;

        if (!IsPathWithinDirectory(fullPath, normalizedSourcePath))
        {
            throw new InvalidOperationException($"Resource file '{resourceName}' references a path outside the skill directory.");
        }

#if NET
        if (!IsSymlinkWithinDirectory(fullPath, normalizedSourcePath))
        {
            throw new InvalidOperationException($"Resource file '{resourceName}' is a symlink that resolves outside the skill directory.");
        }
#endif

        if (!File.Exists(fullPath))
        {
            throw new InvalidOperationException($"Resource file '{resourceName}' not found at '{fullPath}'.");
        }

        LogResourceReading(this._logger, resourceName, skill.Frontmatter.Name);

#if NET
        return await File.ReadAllTextAsync(fullPath, Encoding.UTF8, cancellationToken).ConfigureAwait(false);
#else
        return await Task.FromResult(File.ReadAllText(fullPath, Encoding.UTF8)).ConfigureAwait(false);
#endif
    }

    [LoggerMessage(LogLevel.Information, "Discovered {Count} potential skills")]
    private static partial void LogSkillsDiscovered(ILogger logger, int count);

    [LoggerMessage(LogLevel.Information, "Loaded skill: {SkillName}")]
    private static partial void LogSkillLoaded(ILogger logger, string skillName);

    [LoggerMessage(LogLevel.Information, "Successfully loaded {Count} skills")]
    private static partial void LogSkillsLoadedTotal(ILogger logger, int count);

    [LoggerMessage(LogLevel.Error, "SKILL.md at '{SkillFilePath}' does not contain valid YAML frontmatter delimited by '---'")]
    private static partial void LogInvalidFrontmatter(ILogger logger, string skillFilePath);

    [LoggerMessage(LogLevel.Error, "SKILL.md at '{SkillFilePath}' is missing a '{FieldName}' field in frontmatter")]
    private static partial void LogMissingFrontmatterField(ILogger logger, string skillFilePath, string fieldName);

    [LoggerMessage(LogLevel.Error, "SKILL.md at '{SkillFilePath}' has an invalid '{FieldName}' value: {Reason}")]
    private static partial void LogInvalidFieldValue(ILogger logger, string skillFilePath, string fieldName, string reason);

    [LoggerMessage(LogLevel.Warning, "Excluding skill '{SkillName}': referenced resource '{ResourceName}' does not exist at '{FullPath}'")]
    private static partial void LogMissingResource(ILogger logger, string skillName, string resourceName, string fullPath);

    [LoggerMessage(LogLevel.Warning, "Excluding skill '{SkillName}': resource '{ResourceName}' has disallowed extension '{Extension}'")]
    private static partial void LogDisallowedResourceExtension(ILogger logger, string skillName, string resourceName, string extension);

    [LoggerMessage(LogLevel.Warning, "Excluding skill '{SkillName}': resource '{ResourceName}' is not under any allowed resource path")]
    private static partial void LogDisallowedResourcePath(ILogger logger, string skillName, string resourceName);

    [LoggerMessage(LogLevel.Warning, "Excluding skill '{SkillName}': resource '{ResourceName}' references a path outside the skill directory")]
    private static partial void LogResourcePathTraversal(ILogger logger, string skillName, string resourceName);

    [LoggerMessage(LogLevel.Warning, "Duplicate skill name '{SkillName}': skill from '{ExistingPath}' will be replaced by skill from '{NewPath}'")]
    private static partial void LogDuplicateSkillName(ILogger logger, string skillName, string existingPath, string newPath);

#if NET
    [LoggerMessage(LogLevel.Warning, "Excluding skill '{SkillName}': resource '{ResourceName}' is a symlink that resolves outside the skill directory")]
    private static partial void LogResourceSymlinkEscape(ILogger logger, string skillName, string resourceName);
#endif

    [LoggerMessage(LogLevel.Information, "Reading resource '{FileName}' from skill '{SkillName}'")]
    private static partial void LogResourceReading(ILogger logger, string fileName, string skillName);
}
