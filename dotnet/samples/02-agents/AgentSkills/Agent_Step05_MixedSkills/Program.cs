// Copyright (c) Microsoft. All rights reserved.

// This sample demonstrates how to combine multiple skill types in a single agent using
// the AgentSkillsProviderBuilder. Three different skill sources are registered:
// 1. File-based: unit-converter (miles↔km, pounds↔kg) from SKILL.md on disk
// 2. Code-defined: volume-converter (gallons↔liters) using AgentCodeSkill
// 3. Class-based: temperature-converter (°F↔°C↔K) using AgentClassSkill

using System.Text.Json;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using OpenAI.Responses;

// --- Configuration ---
string endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT")
    ?? throw new InvalidOperationException("AZURE_OPENAI_ENDPOINT is not set.");
string deploymentName = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT_NAME") ?? "gpt-4o-mini";

// --- 1. Code-Defined Skill: volume-converter ---
var volumeConverterSkill = new AgentCodeSkill(
    name: "volume-converter",
    description: "Convert between gallons and liters using a multiplication factor.",
    body: """
        Use this skill when the user asks to convert between gallons and liters.

        1. Review the conversion-table resource to find the correct factor.
        2. Use the convert script, passing the value and factor.
        """)
    .AddResource(
        "conversion-table",
        """
        # Volume Conversion Table

        Formula: **result = value × factor**

        | From    | To      | Factor  |
        |---------|---------|---------|
        | gallons | liters  | 3.78541 |
        | liters  | gallons | 0.264172|
        """)
    .AddScript(ConvertVolume, "convert");

static string ConvertVolume(double value, double factor)
{
    double result = Math.Round(value * factor, 4);
    return JsonSerializer.Serialize(new { value, factor, result });
}

// --- 2. Class-Based Skill: temperature-converter ---
var temperatureConverter = new Agent_Step05_MixedSkills.TemperatureConverterSkill();

// --- 3. Build provider combining all three source types ---
var skillsProvider = new AgentSkillsProviderBuilder()
    .AddFileSkills(Path.Combine(AppContext.BaseDirectory, "skills"))    // File-based: unit-converter
    .AddCodeSkills(volumeConverterSkill)                                // Code-defined: volume-converter
    .AddClassSkills(temperatureConverter)                               // Class-based: temperature-converter
    .Build();

// --- Agent Setup ---
AIAgent agent = new AzureOpenAIClient(new Uri(endpoint), new DefaultAzureCredential())
    .GetResponsesClient(deploymentName)
    .AsAIAgent(new ChatClientAgentOptions
    {
        Name = "MultiConverterAgent",
        ChatOptions = new()
        {
            Instructions = "You are a helpful assistant that can convert units, volumes, and temperatures.",
        },
        AIContextProviders = [skillsProvider],
    });

// --- Example: Use all three skills ---
Console.WriteLine("Converting with mixed skills (file + code + class)");
Console.WriteLine(new string('-', 60));

AgentResponse response = await agent.RunAsync(
    "I need three conversions: " +
    "1) How many kilometers is a marathon (26.2 miles)? " +
    "2) How many liters is a 5-gallon bucket? " +
    "3) What is 98.6°F in Celsius?");

Console.WriteLine($"Agent: {response.Text}");
