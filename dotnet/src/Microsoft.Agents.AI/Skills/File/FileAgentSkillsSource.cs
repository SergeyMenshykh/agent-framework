// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
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
[Experimental(DiagnosticIds.Experiments.AgentsAIExperiments)]
public sealed partial class FileAgentSkillsSource : AgentSkillsSource
{
    private static readonly string[] s_defaultScriptExtensions = [".py", ".js", ".sh", ".ps1", ".cs", ".csx"];

    private readonly IReadOnlyList<string> _skillPaths;
    private readonly IEnumerable<string>? _allowedResourceExtensions;
    private readonly ILoggerFactory? _loggerFactory;
    private readonly FileSkillScriptExecutor? _scriptExecutor;
    private readonly HashSet<string> _scriptExtensionsSet;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileAgentSkillsSource"/> class.
    /// </summary>
    /// <param name="skillPath">Path to search for skills.</param>
    /// <param name="allowedResourceExtensions">Optional resource extension filter.</param>
    /// <param name="loggerFactory">Optional logger factory.</param>
    /// <param name="scriptExecutor">Optional executor for file-based scripts.</param>
    /// <param name="allowedScriptExtensions">Optional script extension filter. Defaults to .py, .js, .sh, .ps1, .cs, .csx.</param>
    public FileAgentSkillsSource(
        string skillPath,
        IEnumerable<string>? allowedResourceExtensions = null,
        ILoggerFactory? loggerFactory = null,
        FileSkillScriptExecutor? scriptExecutor = null,
        IEnumerable<string>? allowedScriptExtensions = null)
        : this([skillPath], allowedResourceExtensions, loggerFactory, scriptExecutor, allowedScriptExtensions)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FileAgentSkillsSource"/> class.
    /// </summary>
    /// <param name="skillPaths">Paths to search for skills.</param>
    /// <param name="allowedResourceExtensions">Optional resource extension filter.</param>
    /// <param name="loggerFactory">Optional logger factory.</param>
    /// <param name="scriptExecutor">Optional executor for file-based scripts.</param>
    /// <param name="allowedScriptExtensions">Optional script extension filter. Defaults to .py, .js, .sh, .ps1, .cs, .csx.</param>
    public FileAgentSkillsSource(
        IEnumerable<string> skillPaths,
        IEnumerable<string>? allowedResourceExtensions = null,
        ILoggerFactory? loggerFactory = null,
        FileSkillScriptExecutor? scriptExecutor = null,
        IEnumerable<string>? allowedScriptExtensions = null)
    {
        _ = Throw.IfNull(skillPaths);

        this._skillPaths = skillPaths.ToList();
        this._allowedResourceExtensions = allowedResourceExtensions;
        this._loggerFactory = loggerFactory;
        this._scriptExecutor = scriptExecutor;
        this._scriptExtensionsSet = new HashSet<string>(
            allowedScriptExtensions ?? s_defaultScriptExtensions,
            StringComparer.OrdinalIgnoreCase);
        this._logger = (loggerFactory ?? NullLoggerFactory.Instance).CreateLogger<FileAgentSkillsSource>();
    }

    /// <inheritdoc/>
    public override Task<IReadOnlyList<AgentSkill>> GetSkillsAsync(CancellationToken cancellationToken = default)
    {
        var loader = new FileAgentSkillLoader(this._logger, this._allowedResourceExtensions);
        var internalSkills = loader.DiscoverAndLoadSkills(this._skillPaths);

        var skills = new List<AgentSkill>(internalSkills.Count);

        foreach (var kvp in internalSkills)
        {
            var resources = new List<AgentSkillResource>(kvp.Value.ResourceNames.Count);
            foreach (string resourceName in kvp.Value.ResourceNames)
            {
                string fullPath = Path.Combine(kvp.Value.SourcePath, resourceName.Replace('/', Path.DirectorySeparatorChar));
                resources.Add(new AgentFileSkillResource(resourceName, fullPath));
            }

            List<string> scriptNames = DiscoverScriptFiles(kvp.Value.SourcePath, kvp.Value.Frontmatter.Name, this._scriptExtensionsSet, this._logger);
            var scripts = new List<AgentSkillScript>(scriptNames.Count);
            foreach (string scriptName in scriptNames)
            {
                var fileScript = new AgentFileSkillScript(scriptName, scriptName, this._scriptExecutor);
                scripts.Add(fileScript);
            }

            var agentSkill = new AgentFileSkill(
                kvp.Value.Frontmatter.Name,
                kvp.Value.Frontmatter.Description,
                kvp.Value.Content,
                kvp.Value.Body,
                kvp.Value.SourcePath,
                resources,
                scripts);

            skills.Add(agentSkill);
        }

        IReadOnlyList<AgentSkill> result = skills;
        return Task.FromResult(result);
    }

    /// <summary>
    /// Scans a skill directory for script files matching the configured extensions.
    /// </summary>
    /// <remarks>
    /// Recursively walks the skill directory and collects files whose extension
    /// matches the allowed set. Each candidate is validated against path-traversal
    /// and symlink-escape checks; unsafe files are skipped with a warning.
    /// </remarks>
    private static List<string> DiscoverScriptFiles(
        string skillDirectoryFullPath,
        string skillName,
        HashSet<string> allowedScriptExtensions,
        ILogger logger)
    {
        string normalizedSkillDirectoryFullPath = skillDirectoryFullPath + Path.DirectorySeparatorChar;
        var scripts = new List<string>();

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
            if (string.IsNullOrEmpty(extension) || !allowedScriptExtensions.Contains(extension))
            {
                continue;
            }

            // Normalize the enumerated path to guard against non-canonical forms
            string resolvedFilePath = Path.GetFullPath(filePath);

            // Path containment check
            if (!resolvedFilePath.StartsWith(normalizedSkillDirectoryFullPath, StringComparison.OrdinalIgnoreCase))
            {
                if (logger.IsEnabled(LogLevel.Warning))
                {
                    LogScriptPathTraversal(logger, skillName, SanitizePathForLog(filePath));
                }

                continue;
            }

            // Symlink check
            if (HasSymlinkInPath(resolvedFilePath, normalizedSkillDirectoryFullPath))
            {
                if (logger.IsEnabled(LogLevel.Warning))
                {
                    LogScriptSymlinkEscape(logger, skillName, SanitizePathForLog(filePath));
                }

                continue;
            }

            // Compute relative path and normalize to forward slashes
            string relativePath = resolvedFilePath.Substring(normalizedSkillDirectoryFullPath.Length);
            scripts.Add(NormalizePath(relativePath));
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

    [LoggerMessage(LogLevel.Warning, "Skipping script in skill '{SkillName}': '{ScriptPath}' references a path outside the skill directory")]
    private static partial void LogScriptPathTraversal(ILogger logger, string skillName, string scriptPath);

    [LoggerMessage(LogLevel.Warning, "Skipping script in skill '{SkillName}': '{ScriptPath}' is a symlink that resolves outside the skill directory")]
    private static partial void LogScriptSymlinkEscape(ILogger logger, string skillName, string scriptPath);
}
