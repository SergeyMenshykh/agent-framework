// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Agents.AI;

/// <summary>
/// A skill source that discovers skills from filesystem directories containing SKILL.md files.
/// </summary>
/// <remarks>
/// Searches directories recursively (up to 2 levels deep) for SKILL.md files.
/// Each file is validated for YAML frontmatter. Resource and script files are discovered by scanning the skill
/// directory for files with matching extensions. Invalid resources are skipped with logged warnings.
/// Resource and script paths are checked against path traversal and symlink escape attacks.
/// </remarks>
[Experimental(DiagnosticIds.Experiments.AgentsAIExperiments)]
public sealed partial class AgentFileSkillsSource : AgentSkillsSource
{
    private const string SkillFileName = "SKILL.md";
    private const int MaxSearchDepth = 2;
    private const int MaxNameLength = 64;
    private const int MaxDescriptionLength = 1024;

    private static readonly string[] s_defaultScriptExtensions = [".py", ".js", ".sh", ".ps1", ".cs", ".csx"];
    private static readonly string[] s_defaultResourceExtensions = [".md", ".json", ".yaml", ".yml", ".csv", ".xml", ".txt"];

    // Matches YAML frontmatter delimited by "---" lines. Group 1 = content between delimiters.
    // Multiline makes ^/$ match line boundaries; Singleline makes . match newlines across the block.
    // The \uFEFF? prefix allows an optional UTF-8 BOM that some editors prepend.
    private static readonly Regex s_frontmatterRegex = new(@"\A\uFEFF?^---\s*$(.+?)^---\s*$", RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.Compiled, TimeSpan.FromSeconds(5));

    // Matches YAML "key: value" lines. Group 1 = key, Group 2 = quoted value, Group 3 = unquoted value.
    // Accepts single or double quotes; the lazy quantifier trims trailing whitespace on unquoted values.
    private static readonly Regex s_yamlKeyValueRegex = new(@"^\s*(\w+)\s*:\s*(?:[""'](.+?)[""']|(.+?))\s*$", RegexOptions.Multiline | RegexOptions.Compiled, TimeSpan.FromSeconds(5));

    // Validates skill names: lowercase letters, numbers, and hyphens only;
    // must not start or end with a hyphen; must not contain consecutive hyphens.
    private static readonly Regex s_validNameRegex = new("^[a-z0-9]([a-z0-9]*-[a-z0-9])*[a-z0-9]*$", RegexOptions.Compiled);

    private readonly IReadOnlyList<string> _skillPaths;
    private readonly HashSet<string> _allowedResourceExtensions;
    private readonly AgentFileSkillScriptExecutor? _scriptExecutor;
    private readonly HashSet<string> _scriptExtensionsSet;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentFileSkillsSource"/> class.
    /// </summary>
    /// <param name="skillPath">Path to search for skills.</param>
    /// <param name="allowedResourceExtensions">Optional resource extension filter.</param>
    /// <param name="loggerFactory">Optional logger factory.</param>
    /// <param name="scriptExecutor">Optional executor for file-based scripts.</param>
    /// <param name="allowedScriptExtensions">Optional script extension filter. Defaults to .py, .js, .sh, .ps1, .cs, .csx.</param>
    public AgentFileSkillsSource(
        string skillPath,
        IEnumerable<string>? allowedResourceExtensions = null,
        ILoggerFactory? loggerFactory = null,
        AgentFileSkillScriptExecutor? scriptExecutor = null,
        IEnumerable<string>? allowedScriptExtensions = null)
        : this([skillPath], allowedResourceExtensions, loggerFactory, scriptExecutor, allowedScriptExtensions)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentFileSkillsSource"/> class.
    /// </summary>
    /// <param name="skillPaths">Paths to search for skills.</param>
    /// <param name="allowedResourceExtensions">Optional resource extension filter.</param>
    /// <param name="loggerFactory">Optional logger factory.</param>
    /// <param name="scriptExecutor">Optional executor for file-based scripts.</param>
    /// <param name="allowedScriptExtensions">Optional script extension filter. Defaults to .py, .js, .sh, .ps1, .cs, .csx.</param>
    public AgentFileSkillsSource(
        IEnumerable<string> skillPaths,
        IEnumerable<string>? allowedResourceExtensions = null,
        ILoggerFactory? loggerFactory = null,
        AgentFileSkillScriptExecutor? scriptExecutor = null,
        IEnumerable<string>? allowedScriptExtensions = null)
    {
        _ = Throw.IfNull(skillPaths);

        ValidateExtensions(allowedResourceExtensions);

        this._skillPaths = skillPaths.ToList();
        this._allowedResourceExtensions = new HashSet<string>(
            allowedResourceExtensions ?? s_defaultResourceExtensions,
            StringComparer.OrdinalIgnoreCase);
        this._scriptExecutor = scriptExecutor;
        this._scriptExtensionsSet = new HashSet<string>(
            allowedScriptExtensions ?? s_defaultScriptExtensions,
            StringComparer.OrdinalIgnoreCase);
        this._logger = (loggerFactory ?? NullLoggerFactory.Instance).CreateLogger<AgentFileSkillsSource>();
    }

    /// <inheritdoc/>
    public override Task<IReadOnlyList<AgentSkill>> GetSkillsAsync(CancellationToken cancellationToken = default)
    {
        var discoveredPaths = DiscoverSkillDirectories(this._skillPaths);

        LogSkillsDiscovered(this._logger, discoveredPaths.Count);

        var skills = new List<AgentSkill>();
        var seenNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (string skillPath in discoveredPaths)
        {
            AgentFileSkill? skill = this.ParseSkillDirectory(skillPath);
            if (skill is null)
            {
                continue;
            }

            if (seenNames.TryGetValue(skill.Name, out string? existingPath))
            {
                LogDuplicateSkillName(this._logger, skill.Name, skillPath, existingPath);
                continue;
            }

            seenNames[skill.Name] = skill.SourcePath;
            skills.Add(skill);

            LogSkillLoaded(this._logger, skill.Name);
        }

        LogSkillsLoadedTotal(this._logger, skills.Count);

        IReadOnlyList<AgentSkill> result = skills;
        return Task.FromResult(result);
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
            results.Add(Path.GetFullPath(directory));
        }

        if (currentDepth >= MaxSearchDepth)
        {
            return;
        }

        foreach (string subdirectory in Directory.EnumerateDirectories(directory))
        {
            SearchDirectoriesForSkills(subdirectory, results, currentDepth + 1);
        }
    }

    private AgentFileSkill? ParseSkillDirectory(string skillDirectoryFullPath)
    {
        string skillFilePath = Path.Combine(skillDirectoryFullPath, SkillFileName);
        string content = File.ReadAllText(skillFilePath, Encoding.UTF8);

        if (!this.TryParseSkillDocument(content, skillFilePath, out string name, out string description))
        {
            return null;
        }

        var resources = this.DiscoverResourceFiles(skillDirectoryFullPath, name);
        var scripts = this.DiscoverScriptFiles(skillDirectoryFullPath, name);

        return new AgentFileSkill(
            name: name,
            description: description,
            content: content,
            sourcePath: skillDirectoryFullPath,
            resources: resources,
            scripts: scripts);
    }

    private bool TryParseSkillDocument(string content, string skillFilePath, out string name, out string description)
    {
        name = null!;
        description = null!;

        Match match = s_frontmatterRegex.Match(content);
        if (!match.Success)
        {
            LogInvalidFrontmatter(this._logger, skillFilePath);
            return false;
        }

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
            LogInvalidFieldValue(this._logger, skillFilePath, "name", $"Must be {MaxNameLength} characters or fewer, using only lowercase letters, numbers, and hyphens, and must not start or end with a hyphen or contain consecutive hyphens.");
            return false;
        }

        // skillFilePath is e.g. "/skills/my-skill/SKILL.md".
        // GetDirectoryName strips the filename → "/skills/my-skill".
        // GetFileName then extracts the last segment → "my-skill".
        // This gives us the skill's parent directory name to validate against the frontmatter name.
        string directoryName = Path.GetFileName(Path.GetDirectoryName(skillFilePath)) ?? string.Empty;
        if (!string.Equals(name, directoryName, StringComparison.Ordinal))
        {
            if (this._logger.IsEnabled(LogLevel.Error))
            {
                LogNameDirectoryMismatch(this._logger, SanitizePathForLog(skillFilePath), name, SanitizePathForLog(directoryName));
            }

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

        return true;
    }

    /// <summary>
    /// Scans a skill directory for resource files matching the configured extensions.
    /// </summary>
    /// <remarks>
    /// Recursively walks <paramref name="skillDirectoryFullPath"/> and collects files whose extension
    /// matches the allowed set, excluding <c>SKILL.md</c> itself. Each candidate
    /// is validated against path-traversal and symlink-escape checks; unsafe files are skipped with
    /// a warning.
    /// </remarks>
    private List<AgentFileSkillResource> DiscoverResourceFiles(string skillDirectoryFullPath, string skillName)
    {
        string normalizedSkillDirectoryFullPath = skillDirectoryFullPath + Path.DirectorySeparatorChar;

        var resources = new List<AgentFileSkillResource>();

#if NET
        var enumerationOptions = new EnumerationOptions
        {
            RecurseSubdirectories = true,
            IgnoreInaccessible = true,
            AttributesToSkip = FileAttributes.ReparsePoint,
        };

        foreach (string filePath in Directory.EnumerateFiles(skillDirectoryFullPath, "*", enumerationOptions))
#else
        foreach (string filePath in Directory.EnumerateFiles(skillDirectoryFullPath, "*", SearchOption.AllDirectories))
#endif
        {
            string fileName = Path.GetFileName(filePath);

            // Exclude SKILL.md itself
            if (string.Equals(fileName, SkillFileName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            // Filter by extension
            string extension = Path.GetExtension(filePath);
            if (string.IsNullOrEmpty(extension) || !this._allowedResourceExtensions.Contains(extension))
            {
                if (this._logger.IsEnabled(LogLevel.Debug))
                {
                    LogResourceSkippedExtension(this._logger, skillName, SanitizePathForLog(filePath), extension);
                }

                continue;
            }

            // Normalize the enumerated path to guard against non-canonical forms
            string resolvedFilePath = Path.GetFullPath(filePath);

            // Path containment check
            if (!resolvedFilePath.StartsWith(normalizedSkillDirectoryFullPath, StringComparison.OrdinalIgnoreCase))
            {
                if (this._logger.IsEnabled(LogLevel.Warning))
                {
                    LogResourcePathTraversal(this._logger, skillName, SanitizePathForLog(filePath));
                }

                continue;
            }

            // Symlink check
            if (HasSymlinkInPath(resolvedFilePath, normalizedSkillDirectoryFullPath))
            {
                if (this._logger.IsEnabled(LogLevel.Warning))
                {
                    LogResourceSymlinkEscape(this._logger, skillName, SanitizePathForLog(filePath));
                }

                continue;
            }

            // Compute relative path and normalize to forward slashes
            string relativePath = NormalizePath(resolvedFilePath.Substring(normalizedSkillDirectoryFullPath.Length));
            resources.Add(new AgentFileSkillResource(relativePath, resolvedFilePath));
        }

        return resources;
    }

    /// <summary>
    /// Scans a skill directory for script files matching the configured extensions.
    /// </summary>
    /// <remarks>
    /// Recursively walks the skill directory and collects files whose extension
    /// matches the allowed set. Each candidate is validated against path-traversal
    /// and symlink-escape checks; unsafe files are skipped with a warning.
    /// </remarks>
    private List<AgentFileSkillScript> DiscoverScriptFiles(string skillDirectoryFullPath, string skillName)
    {
        string normalizedSkillDirectoryFullPath = skillDirectoryFullPath + Path.DirectorySeparatorChar;
        var scripts = new List<AgentFileSkillScript>();

#if NET
        var enumerationOptions = new EnumerationOptions
        {
            RecurseSubdirectories = true,
            IgnoreInaccessible = true,
            AttributesToSkip = FileAttributes.ReparsePoint,
        };

        foreach (string filePath in Directory.EnumerateFiles(skillDirectoryFullPath, "*", enumerationOptions))
#else
        foreach (string filePath in Directory.EnumerateFiles(skillDirectoryFullPath, "*", SearchOption.AllDirectories))
#endif
        {
            // Filter by extension
            string extension = Path.GetExtension(filePath);
            if (string.IsNullOrEmpty(extension) || !this._scriptExtensionsSet.Contains(extension))
            {
                continue;
            }

            // Normalize the enumerated path to guard against non-canonical forms
            string resolvedFilePath = Path.GetFullPath(filePath);

            // Path containment check
            if (!resolvedFilePath.StartsWith(normalizedSkillDirectoryFullPath, StringComparison.OrdinalIgnoreCase))
            {
                if (this._logger.IsEnabled(LogLevel.Warning))
                {
                    LogScriptPathTraversal(this._logger, skillName, SanitizePathForLog(filePath));
                }

                continue;
            }

            // Symlink check
            if (HasSymlinkInPath(resolvedFilePath, normalizedSkillDirectoryFullPath))
            {
                if (this._logger.IsEnabled(LogLevel.Warning))
                {
                    LogScriptSymlinkEscape(this._logger, skillName, SanitizePathForLog(filePath));
                }

                continue;
            }

            // Compute relative path and normalize to forward slashes
            string relativePath = NormalizePath(resolvedFilePath.Substring(normalizedSkillDirectoryFullPath.Length));
            scripts.Add(new AgentFileSkillScript(relativePath, relativePath, this._scriptExecutor));
        }

        return scripts;
    }

    /// <summary>
    /// Checks whether any segment in the path (relative to the directory) is a symlink.
    /// </summary>
    private static bool HasSymlinkInPath(string fullPath, string normalizedDirectoryPath)
    {
        string relativePath = fullPath.Substring(normalizedDirectoryPath.Length);
        string[] segments = relativePath.Split(
            new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar },
            StringSplitOptions.RemoveEmptyEntries);

        string currentPath = normalizedDirectoryPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        foreach (string segment in segments)
        {
            currentPath = Path.Combine(currentPath, segment);

            if ((File.GetAttributes(currentPath) & FileAttributes.ReparsePoint) != 0)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Normalizes a relative path by replacing backslashes with forward slashes
    /// and trimming a leading "./" prefix.
    /// </summary>
    private static string NormalizePath(string path)
    {
        if (path.IndexOf('\\') >= 0)
        {
            path = path.Replace('\\', '/');
        }

        if (path.StartsWith("./", StringComparison.Ordinal))
        {
            path = path.Substring(2);
        }

        return path;
    }

    /// <summary>
    /// Replaces control characters in a file path with '?' to prevent log injection.
    /// </summary>
    private static string SanitizePathForLog(string path)
    {
        char[]? chars = null;
        for (int i = 0; i < path.Length; i++)
        {
            if (char.IsControl(path[i]))
            {
                chars ??= path.ToCharArray();
                chars[i] = '?';
            }
        }

        return chars is null ? path : new string(chars);
    }

    private static void ValidateExtensions(IEnumerable<string>? extensions)
    {
        if (extensions is null)
        {
            return;
        }

        foreach (string ext in extensions)
        {
            if (string.IsNullOrWhiteSpace(ext) || !ext.StartsWith(".", StringComparison.Ordinal))
            {
#pragma warning disable CA2208 // Instantiate argument exceptions correctly
                throw new ArgumentException($"Each extension must start with '.'. Invalid value: '{ext}'", "allowedResourceExtensions");
#pragma warning restore CA2208 // Instantiate argument exceptions correctly
            }
        }
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

    [LoggerMessage(LogLevel.Error, "SKILL.md at '{SkillFilePath}': skill name '{SkillName}' does not match parent directory name '{DirectoryName}'")]
    private static partial void LogNameDirectoryMismatch(ILogger logger, string skillFilePath, string skillName, string directoryName);

    [LoggerMessage(LogLevel.Warning, "Skipping resource in skill '{SkillName}': '{ResourcePath}' references a path outside the skill directory")]
    private static partial void LogResourcePathTraversal(ILogger logger, string skillName, string resourcePath);

    [LoggerMessage(LogLevel.Warning, "Duplicate skill name '{SkillName}': skill from '{NewPath}' skipped in favor of existing skill from '{ExistingPath}'")]
    private static partial void LogDuplicateSkillName(ILogger logger, string skillName, string newPath, string existingPath);

    [LoggerMessage(LogLevel.Warning, "Skipping resource in skill '{SkillName}': '{ResourcePath}' is a symlink that resolves outside the skill directory")]
    private static partial void LogResourceSymlinkEscape(ILogger logger, string skillName, string resourcePath);

    [LoggerMessage(LogLevel.Debug, "Skipping file '{FilePath}' in skill '{SkillName}': extension '{Extension}' is not in the allowed list")]
    private static partial void LogResourceSkippedExtension(ILogger logger, string skillName, string filePath, string extension);

    [LoggerMessage(LogLevel.Warning, "Skipping script in skill '{SkillName}': '{ScriptPath}' references a path outside the skill directory")]
    private static partial void LogScriptPathTraversal(ILogger logger, string skillName, string scriptPath);

    [LoggerMessage(LogLevel.Warning, "Skipping script in skill '{SkillName}': '{ScriptPath}' is a symlink that resolves outside the skill directory")]
    private static partial void LogScriptSymlinkEscape(ILogger logger, string skillName, string scriptPath);
}
