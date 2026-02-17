// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Agents.AI;

/// <summary>
/// Discovers Agent Skills from filesystem directories.
/// </summary>
/// <remarks>
/// Skills are discovered by scanning directories for SKILL.md files.
/// Each directory containing a SKILL.md file is considered a potential skill.
/// </remarks>
internal static class SkillDiscovery
{
    private const string SkillFileName = "SKILL.md";

    /// <summary>
    /// Discovers skills from the specified root directories.
    /// </summary>
    /// <param name="skillDirectories">The root directories to search for skills.</param>
    /// <returns>A collection of paths to directories containing SKILL.md files.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="skillDirectories"/> is <see langword="null"/>.</exception>
    public static IReadOnlyList<string> DiscoverSkills(IEnumerable<string> skillDirectories)
    {
        _ = Throw.IfNull(skillDirectories);

        var discoveredSkillPaths = new List<string>();

        foreach (string rootDirectory in skillDirectories)
        {
            if (string.IsNullOrWhiteSpace(rootDirectory))
            {
                continue;
            }

            if (!Directory.Exists(rootDirectory))
            {
                continue;
            }

            // Find all SKILL.md files in the root directory and subdirectories
            var skillFiles = Directory.GetFiles(
                rootDirectory,
                SkillFileName,
                SearchOption.AllDirectories);

            // Extract the directory paths
            foreach (string skillFile in skillFiles)
            {
                string? directory = Path.GetDirectoryName(skillFile);
                if (!string.IsNullOrEmpty(directory))
                {
                    discoveredSkillPaths.Add(directory);
                }
            }
        }

        return discoveredSkillPaths;
    }

    /// <summary>
    /// Checks if a directory contains a SKILL.md file.
    /// </summary>
    /// <param name="directoryPath">The directory path to check.</param>
    /// <returns><see langword="true"/> if the directory contains a SKILL.md file; otherwise, <see langword="false"/>.</returns>
    public static bool ContainsSkillFile(string directoryPath)
    {
        if (string.IsNullOrWhiteSpace(directoryPath) || !Directory.Exists(directoryPath))
        {
            return false;
        }

        string skillFilePath = Path.Combine(directoryPath, SkillFileName);
        return File.Exists(skillFilePath);
    }

    /// <summary>
    /// Gets the path to the SKILL.md file in the specified directory.
    /// </summary>
    /// <param name="directoryPath">The directory path.</param>
    /// <returns>The full path to the SKILL.md file.</returns>
    public static string GetSkillFilePath(string directoryPath)
    {
        return Path.Combine(directoryPath, SkillFileName);
    }
}
