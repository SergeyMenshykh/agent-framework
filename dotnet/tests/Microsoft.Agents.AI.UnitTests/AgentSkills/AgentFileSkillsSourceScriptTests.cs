// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.AI;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Agents.AI.UnitTests.AgentSkills;

/// <summary>
/// Unit tests for script discovery and execution in <see cref="AgentFileSkillsSource"/>.
/// </summary>
public sealed class AgentFileSkillsSourceScriptTests : IDisposable
{
    private static readonly string[] s_rubyExtension = new[] { ".rb" };

    private readonly string _testRoot;

    public AgentFileSkillsSourceScriptTests()
    {
        this._testRoot = Path.Combine(Path.GetTempPath(), "skills-source-script-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(this._testRoot);
    }

    public void Dispose()
    {
        if (Directory.Exists(this._testRoot))
        {
            Directory.Delete(this._testRoot, recursive: true);
        }
    }

    [Fact]
    public async Task GetSkillsAsync_WithScriptFiles_DiscoversScriptsAsync()
    {
        // Arrange
        CreateSkillWithScript(this._testRoot, "my-skill", "A test skill", "Body.", "scripts/convert.py", "print('hello')");
        var source = new AgentFileSkillsSource(this._testRoot);

        // Act
        var skills = await source.GetSkillsAsync(CancellationToken.None);

        // Assert
        Assert.Single(skills);
        var skill = skills[0];
        Assert.NotNull(skill.Scripts);
        Assert.Single(skill.Scripts!);
        Assert.Equal("scripts/convert.py", skill.Scripts![0].Name);
    }

    [Fact]
    public async Task GetSkillsAsync_WithMultipleScriptExtensions_DiscoversAllAsync()
    {
        // Arrange
        string skillDir = CreateSkillDir(this._testRoot, "multi-ext-skill", "Multi-extension skill", "Body.");
        CreateFile(skillDir, "scripts/run.py", "print('py')");
        CreateFile(skillDir, "scripts/run.sh", "echo 'sh'");
        CreateFile(skillDir, "scripts/run.js", "console.log('js')");
        CreateFile(skillDir, "scripts/run.ps1", "Write-Host 'ps'");
        CreateFile(skillDir, "scripts/run.cs", "Console.WriteLine();");
        CreateFile(skillDir, "scripts/run.csx", "Console.WriteLine();");
        var source = new AgentFileSkillsSource(this._testRoot);

        // Act
        var skills = await source.GetSkillsAsync(CancellationToken.None);

        // Assert
        Assert.Single(skills);
        var scriptNames = skills[0].Scripts!.Select(s => s.Name).OrderBy(n => n, StringComparer.Ordinal).ToList();
        Assert.Equal(6, scriptNames.Count);
        Assert.Contains("scripts/run.cs", scriptNames);
        Assert.Contains("scripts/run.csx", scriptNames);
        Assert.Contains("scripts/run.js", scriptNames);
        Assert.Contains("scripts/run.ps1", scriptNames);
        Assert.Contains("scripts/run.py", scriptNames);
        Assert.Contains("scripts/run.sh", scriptNames);
    }

    [Fact]
    public async Task GetSkillsAsync_NonScriptExtensionsAreNotDiscoveredAsync()
    {
        // Arrange
        string skillDir = CreateSkillDir(this._testRoot, "no-script-skill", "Non-script skill", "Body.");
        CreateFile(skillDir, "scripts/data.txt", "text data");
        CreateFile(skillDir, "scripts/config.json", "{}");
        CreateFile(skillDir, "scripts/notes.md", "# Notes");
        var source = new AgentFileSkillsSource(this._testRoot);

        // Act
        var skills = await source.GetSkillsAsync(CancellationToken.None);

        // Assert
        Assert.Single(skills);
        Assert.Empty(skills[0].Scripts!);
    }

    [Fact]
    public async Task GetSkillsAsync_NoScriptFiles_ReturnsEmptyScriptsAsync()
    {
        // Arrange
        CreateSkillDir(this._testRoot, "no-scripts", "No scripts skill", "Body.");
        var source = new AgentFileSkillsSource(this._testRoot);

        // Act
        var skills = await source.GetSkillsAsync(CancellationToken.None);

        // Assert
        Assert.Single(skills);
        Assert.NotNull(skills[0].Scripts);
        Assert.Empty(skills[0].Scripts!);
    }

    [Fact]
    public async Task GetSkillsAsync_ScriptsOutsideScriptsDir_AreAlsoDiscoveredAsync()
    {
        // Arrange — scripts at any depth in the skill directory are discovered
        string skillDir = CreateSkillDir(this._testRoot, "root-scripts", "Root scripts skill", "Body.");
        CreateFile(skillDir, "convert.py", "print('root')");
        CreateFile(skillDir, "tools/helper.sh", "echo 'helper'");
        var source = new AgentFileSkillsSource(this._testRoot);

        // Act
        var skills = await source.GetSkillsAsync(CancellationToken.None);

        // Assert
        Assert.Single(skills);
        var scriptNames = skills[0].Scripts!.Select(s => s.Name).OrderBy(n => n, StringComparer.Ordinal).ToList();
        Assert.Equal(2, scriptNames.Count);
        Assert.Contains("convert.py", scriptNames);
        Assert.Contains("tools/helper.sh", scriptNames);
    }

    [Fact]
    public async Task GetSkillsAsync_WithExecutor_ScriptsCanExecuteAsync()
    {
        // Arrange
        CreateSkillWithScript(this._testRoot, "exec-skill", "Executor test", "Body.", "scripts/test.py", "print('ok')");
        var executorCalled = false;
        var source = new AgentFileSkillsSource(
            this._testRoot,
            scriptExecutor: (skill, script, args, ct) =>
            {
                executorCalled = true;
                Assert.Equal("exec-skill", skill.Frontmatter.Name);
                Assert.Equal("scripts/test.py", script.Name);
                Assert.Equal("scripts/test.py", script.Path);
                return Task.FromResult<object?>("executed");
            });

        // Act
        var skills = await source.GetSkillsAsync(CancellationToken.None);
        var scriptResult = await skills[0].Scripts![0].ExecuteAsync(skills[0], new Microsoft.Extensions.AI.AIFunctionArguments(), CancellationToken.None);

        // Assert
        Assert.True(executorCalled);
        Assert.Equal("executed", scriptResult);
    }

    [Fact]
    public async Task GetSkillsAsync_WithoutExecutor_ScriptThrowsNotSupportedAsync()
    {
        // Arrange
        CreateSkillWithScript(this._testRoot, "no-exec-skill", "No executor test", "Body.", "scripts/test.py", "print('ok')");
        var source = new AgentFileSkillsSource(this._testRoot);

        // Act
        var skills = await source.GetSkillsAsync(CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<NotSupportedException>(() =>
            skills[0].Scripts![0].ExecuteAsync(skills[0], new Microsoft.Extensions.AI.AIFunctionArguments(), CancellationToken.None));
    }

    [Fact]
    public async Task GetSkillsAsync_CustomScriptExtensions_OnlyDiscoversMatchingAsync()
    {
        // Arrange
        string skillDir = CreateSkillDir(this._testRoot, "custom-ext-skill", "Custom extensions", "Body.");
        CreateFile(skillDir, "scripts/run.py", "print('py')");
        CreateFile(skillDir, "scripts/run.rb", "puts 'rb'");
        var source = new AgentFileSkillsSource(this._testRoot, allowedScriptExtensions: s_rubyExtension);

        // Act
        var skills = await source.GetSkillsAsync(CancellationToken.None);

        // Assert
        Assert.Single(skills);
        Assert.Single(skills[0].Scripts!);
        Assert.Equal("scripts/run.rb", skills[0].Scripts![0].Name);
    }

    [Fact]
    public async Task GetSkillsAsync_ExecutorReceivesArgumentsAsync()
    {
        // Arrange
        CreateSkillWithScript(this._testRoot, "args-skill", "Args test", "Body.", "scripts/test.py", "print('ok')");
        AIFunctionArguments? capturedArgs = null;
        var source = new AgentFileSkillsSource(
            this._testRoot,
            scriptExecutor: (skill, script, args, ct) =>
            {
                capturedArgs = args;
                return Task.FromResult<object?>("done");
            });

        // Act
        var skills = await source.GetSkillsAsync(CancellationToken.None);
        var arguments = new Microsoft.Extensions.AI.AIFunctionArguments
        {
            ["value"] = 26.2,
            ["factor"] = 1.60934
        };
        await skills[0].Scripts![0].ExecuteAsync(skills[0], arguments, CancellationToken.None);

        // Assert
        Assert.NotNull(capturedArgs);
        Assert.Equal(26.2, capturedArgs["value"]);
        Assert.Equal(1.60934, capturedArgs["factor"]);
    }

    private static string CreateSkillDir(string root, string name, string description, string body)
    {
        string skillDir = Path.Combine(root, name);
        Directory.CreateDirectory(skillDir);
        File.WriteAllText(
            Path.Combine(skillDir, "SKILL.md"),
            $"---\nname: {name}\ndescription: {description}\n---\n{body}");
        return skillDir;
    }

    private static void CreateSkillWithScript(string root, string name, string description, string body, string scriptRelativePath, string scriptContent)
    {
        string skillDir = CreateSkillDir(root, name, description, body);
        CreateFile(skillDir, scriptRelativePath, scriptContent);
    }

    private static void CreateFile(string root, string relativePath, string content)
    {
        string fullPath = Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, content);
    }
}
