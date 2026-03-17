// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;

namespace Microsoft.Agents.AI.UnitTests.AgentSkills;

/// <summary>
/// Unit tests for the <see cref="AgentSkillsProvider"/> class with <see cref="AgentFileSkillsSource"/>.
/// </summary>
public sealed class AgentSkillsProviderTests : IDisposable
{
    private readonly string _testRoot;
    private readonly TestAIAgent _agent = new();

    public AgentSkillsProviderTests()
    {
        this._testRoot = Path.Combine(Path.GetTempPath(), "skills-provider-tests-" + Guid.NewGuid().ToString("N"));
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
    public async Task InvokingCoreAsync_NoSkills_ReturnsInputContextUnchangedAsync()
    {
        // Arrange
        var provider = new AgentSkillsProvider(new AgentFileSkillsSource(this._testRoot));
        var inputContext = new AIContext { Instructions = "Original instructions" };
        var invokingContext = new AIContextProvider.InvokingContext(this._agent, session: null, inputContext);

        // Act
        var result = await provider.InvokingAsync(invokingContext, CancellationToken.None);

        // Assert
        Assert.Equal("Original instructions", result.Instructions);
        Assert.Null(result.Tools);
    }

    [Fact]
    public async Task InvokingCoreAsync_WithSkills_AppendsInstructionsAndToolsAsync()
    {
        // Arrange
        this.CreateSkill("provider-skill", "Provider skill test", "Skill instructions body.");
        var provider = new AgentSkillsProvider(new AgentFileSkillsSource(this._testRoot));
        var inputContext = new AIContext { Instructions = "Base instructions" };
        var invokingContext = new AIContextProvider.InvokingContext(this._agent, session: null, inputContext);

        // Act
        var result = await provider.InvokingAsync(invokingContext, CancellationToken.None);

        // Assert
        Assert.NotNull(result.Instructions);
        Assert.Contains("Base instructions", result.Instructions);
        Assert.Contains("provider-skill", result.Instructions);
        Assert.Contains("Provider skill test", result.Instructions);

        // Should have load_skill and read_skill_resource tools
        Assert.NotNull(result.Tools);
        var toolNames = result.Tools!.Select(t => t.Name).ToList();
        Assert.Contains("load_skill", toolNames);
        Assert.Contains("read_skill_resource", toolNames);
    }

    [Fact]
    public async Task InvokingCoreAsync_NullInputInstructions_SetsInstructionsAsync()
    {
        // Arrange
        this.CreateSkill("null-instr-skill", "Null instruction test", "Body.");
        var provider = new AgentSkillsProvider(new AgentFileSkillsSource(this._testRoot));
        var inputContext = new AIContext();
        var invokingContext = new AIContextProvider.InvokingContext(this._agent, session: null, inputContext);

        // Act
        var result = await provider.InvokingAsync(invokingContext, CancellationToken.None);

        // Assert
        Assert.NotNull(result.Instructions);
        Assert.Contains("null-instr-skill", result.Instructions);
    }

    [Fact]
    public async Task InvokingCoreAsync_CustomPromptTemplate_UsesCustomTemplateAsync()
    {
        // Arrange
        this.CreateSkill("custom-prompt-skill", "Custom prompt", "Body.");
        var options = new AgentSkillsProviderOptions
        {
            SkillsInstructionPrompt = "Custom template: {0}"
        };
        var provider = new AgentSkillsProvider(new AgentFileSkillsSource(this._testRoot), options);
        var inputContext = new AIContext();
        var invokingContext = new AIContextProvider.InvokingContext(this._agent, session: null, inputContext);

        // Act
        var result = await provider.InvokingAsync(invokingContext, CancellationToken.None);

        // Assert
        Assert.NotNull(result.Instructions);
        Assert.StartsWith("Custom template:", result.Instructions);
        Assert.Contains("custom-prompt-skill", result.Instructions);
        Assert.Contains("Custom prompt", result.Instructions);
    }

    [Fact]
    public async Task InvokingCoreAsync_SkillNamesAreXmlEscapedAsync()
    {
        // Arrange — description with XML-sensitive characters
        string skillDir = Path.Combine(this._testRoot, "xml-skill");
        Directory.CreateDirectory(skillDir);
        File.WriteAllText(
            Path.Combine(skillDir, "SKILL.md"),
            "---\nname: xml-skill\ndescription: Uses <tags> & \"quotes\"\n---\nBody.");
        var provider = new AgentSkillsProvider(new AgentFileSkillsSource(this._testRoot));
        var inputContext = new AIContext();
        var invokingContext = new AIContextProvider.InvokingContext(this._agent, session: null, inputContext);

        // Act
        var result = await provider.InvokingAsync(invokingContext, CancellationToken.None);

        // Assert
        Assert.NotNull(result.Instructions);
        Assert.Contains("&lt;tags&gt;", result.Instructions);
        Assert.Contains("&amp;", result.Instructions);
    }

    [Fact]
    public async Task Constructor_WithMultiplePaths_LoadsFromAllAsync()
    {
        // Arrange
        string dir1 = Path.Combine(this._testRoot, "dir1");
        string dir2 = Path.Combine(this._testRoot, "dir2");
        CreateSkillIn(dir1, "skill-a", "Skill A", "Body A.");
        CreateSkillIn(dir2, "skill-b", "Skill B", "Body B.");

        // Act
        var provider = new AgentSkillsProvider(new AgentFileSkillsSource(new[] { dir1, dir2 }));
        var invokingContext = new AIContextProvider.InvokingContext(this._agent, session: null, new AIContext());

        // Assert
        var result = await provider.InvokingAsync(invokingContext, CancellationToken.None);
        Assert.NotNull(result.Instructions);
        Assert.Contains("skill-a", result.Instructions);
        Assert.Contains("skill-b", result.Instructions);
    }

    [Fact]
    public async Task InvokingCoreAsync_PreservesExistingInputToolsAsync()
    {
        // Arrange
        this.CreateSkill("tools-skill", "Tools test", "Body.");
        var provider = new AgentSkillsProvider(new AgentFileSkillsSource(this._testRoot));

        var existingTool = AIFunctionFactory.Create(() => "test", name: "existing_tool", description: "An existing tool.");
        var inputContext = new AIContext { Tools = new[] { existingTool } };
        var invokingContext = new AIContextProvider.InvokingContext(this._agent, session: null, inputContext);

        // Act
        var result = await provider.InvokingAsync(invokingContext, CancellationToken.None);

        // Assert — existing tool should be preserved alongside the new skill tools
        Assert.NotNull(result.Tools);
        var toolNames = result.Tools!.Select(t => t.Name).ToList();
        Assert.Contains("existing_tool", toolNames);
        Assert.Contains("load_skill", toolNames);
        Assert.Contains("read_skill_resource", toolNames);
    }

    [Fact]
    public async Task InvokingCoreAsync_SkillsListIsSortedByNameAsync()
    {
        // Arrange — create skills in reverse alphabetical order
        this.CreateSkill("zulu-skill", "Zulu skill", "Body Z.");
        this.CreateSkill("alpha-skill", "Alpha skill", "Body A.");
        this.CreateSkill("mike-skill", "Mike skill", "Body M.");
        var provider = new AgentSkillsProvider(new AgentFileSkillsSource(this._testRoot));
        var inputContext = new AIContext();
        var invokingContext = new AIContextProvider.InvokingContext(this._agent, session: null, inputContext);

        // Act
        var result = await provider.InvokingAsync(invokingContext, CancellationToken.None);

        // Assert — skills should appear in alphabetical order in the prompt
        Assert.NotNull(result.Instructions);
        int alphaIndex = result.Instructions!.IndexOf("alpha-skill", StringComparison.Ordinal);
        int mikeIndex = result.Instructions.IndexOf("mike-skill", StringComparison.Ordinal);
        int zuluIndex = result.Instructions.IndexOf("zulu-skill", StringComparison.Ordinal);
        Assert.True(alphaIndex < mikeIndex, "alpha-skill should appear before mike-skill");
        Assert.True(mikeIndex < zuluIndex, "mike-skill should appear before zulu-skill");
    }

    [Fact]
    public async Task ProvideAIContextAsync_ConcurrentCalls_LoadsSkillsOnlyOnceAsync()
    {
        // Arrange
        var source = new CountingAgentSkillsSource(
        [
            new AgentCodeSkill("concurrent-skill", "Concurrent test", "Body.")
        ]);
        var provider = new AgentSkillsProvider(source);

        var invokingContext = new AIContextProvider.InvokingContext(this._agent, session: null, new AIContext());

        // Act — invoke concurrently from multiple threads
        var tasks = Enumerable.Range(0, 10)
            .Select(_ => provider.InvokingAsync(invokingContext, CancellationToken.None).AsTask())
            .ToArray();
        await Task.WhenAll(tasks);

        // Assert — GetSkillsAsync should have been called exactly once
        Assert.Equal(1, source.GetSkillsCallCount);
    }

    [Fact]
    public async Task InvokingCoreAsync_WithScripts_IncludesRunSkillScriptToolAsync()
    {
        // Arrange
        string skillDir = Path.Combine(this._testRoot, "script-skill");
        Directory.CreateDirectory(Path.Combine(skillDir, "scripts"));
        File.WriteAllText(
            Path.Combine(skillDir, "SKILL.md"),
            "---\nname: script-skill\ndescription: Skill with scripts\n---\nBody.");
        File.WriteAllText(
            Path.Combine(skillDir, "scripts", "test.py"),
            "print('hello')");

        var source = new AgentFileSkillsSource(this._testRoot);
        var provider = new AgentSkillsProvider(source);
        var inputContext = new AIContext();
        var invokingContext = new AIContextProvider.InvokingContext(this._agent, session: null, inputContext);

        // Act
        var result = await provider.InvokingAsync(invokingContext, CancellationToken.None);

        // Assert
        Assert.NotNull(result.Tools);
        var toolNames = result.Tools!.Select(t => t.Name).ToList();
        Assert.Contains("run_skill_script", toolNames);
        Assert.Contains("load_skill", toolNames);
        Assert.Contains("read_skill_resource", toolNames);
    }

    [Fact]
    public async Task InvokingCoreAsync_WithoutScripts_NoRunSkillScriptToolAsync()
    {
        // Arrange
        this.CreateSkill("no-script-skill", "No scripts", "Body.");
        var source = new AgentFileSkillsSource(this._testRoot);
        var provider = new AgentSkillsProvider(source);
        var inputContext = new AIContext();
        var invokingContext = new AIContextProvider.InvokingContext(this._agent, session: null, inputContext);

        // Act
        var result = await provider.InvokingAsync(invokingContext, CancellationToken.None);

        // Assert
        Assert.NotNull(result.Tools);
        var toolNames = result.Tools!.Select(t => t.Name).ToList();
        Assert.DoesNotContain("run_skill_script", toolNames);
    }

    private void CreateSkill(string name, string description, string body)
    {
        CreateSkillIn(this._testRoot, name, description, body);
    }

    [Fact]
    public async Task LoadSkill_DefaultOptions_ReturnsFullContentAsync()
    {
        // Arrange
        this.CreateSkill("content-skill", "Content test", "Skill body.");
        var provider = new AgentSkillsProvider(new AgentFileSkillsSource(this._testRoot));
        var inputContext = new AIContext();
        var invokingContext = new AIContextProvider.InvokingContext(this._agent, session: null, inputContext);
        var result = await provider.InvokingAsync(invokingContext, CancellationToken.None);
        var loadSkillTool = result.Tools!.First(t => t.Name == "load_skill") as AIFunction;

        // Act
        var content = await loadSkillTool!.InvokeAsync(new AIFunctionArguments(new Dictionary<string, object?> { ["skillName"] = "content-skill" }));

        // Assert — should contain frontmatter and body
        var text = content!.ToString()!;
        Assert.Contains("---", text);
        Assert.Contains("name: content-skill", text);
        Assert.Contains("Skill body.", text);
    }

    [Fact]
    public async Task Builder_WithFileScriptExecutorAfterAddFileSkills_ExecutorIsUsedAsync()
    {
        // Arrange — create a skill with a script file
        string skillDir = Path.Combine(this._testRoot, "builder-skill");
        Directory.CreateDirectory(Path.Combine(skillDir, "scripts"));
        File.WriteAllText(
            Path.Combine(skillDir, "SKILL.md"),
            "---\nname: builder-skill\ndescription: Builder test\n---\nBody.");
        File.WriteAllText(
            Path.Combine(skillDir, "scripts", "run.py"),
            "print('ok')");

        var executorCalled = false;

        // Act — call WithFileScriptExecutor AFTER AddFileSkills (the bug scenario)
        var provider = new AgentSkillsProviderBuilder()
            .AddFileSkills(this._testRoot)
            .WithFileScriptExecutor((skill, script, args, ct) =>
            {
                executorCalled = true;
                return Task.FromResult<object?>("executed");
            })
            .Build();

        var invokingContext = new AIContextProvider.InvokingContext(this._agent, session: null, new AIContext());
        var result = await provider.InvokingAsync(invokingContext, CancellationToken.None);

        // Assert — run_skill_script tool should be present and executor should work
        Assert.NotNull(result.Tools);
        var toolNames = result.Tools!.Select(t => t.Name).ToList();
        Assert.Contains("run_skill_script", toolNames);

        var runScriptTool = result.Tools!.First(t => t.Name == "run_skill_script") as AIFunction;
        await runScriptTool!.InvokeAsync(new AIFunctionArguments(new Dictionary<string, object?>
        {
            ["skillName"] = "builder-skill",
            ["scriptName"] = "scripts/run.py",
        }));

        Assert.True(executorCalled);
    }

    private static void CreateSkillIn(string root, string name, string description, string body)
    {
        string skillDir = Path.Combine(root, name);
        Directory.CreateDirectory(skillDir);
        File.WriteAllText(
            Path.Combine(skillDir, "SKILL.md"),
            $"---\nname: {name}\ndescription: {description}\n---\n{body}");
    }

    /// <summary>
    /// A test skill source that counts how many times <see cref="GetSkillsAsync"/> is called.
    /// </summary>
    private sealed class CountingAgentSkillsSource : AgentSkillsSource
    {
        private readonly IReadOnlyList<AgentSkill> _skills;
        private int _callCount;

        public CountingAgentSkillsSource(IReadOnlyList<AgentSkill> skills)
        {
            this._skills = skills;
        }

        public int GetSkillsCallCount => this._callCount;

        public override Task<IReadOnlyList<AgentSkill>> GetSkillsAsync(CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref this._callCount);
            return Task.FromResult(this._skills);
        }
    }
}
