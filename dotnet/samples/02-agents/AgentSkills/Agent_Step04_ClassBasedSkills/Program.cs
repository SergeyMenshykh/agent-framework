// Copyright (c) Microsoft. All rights reserved.

// This sample demonstrates how to define Agent Skills as reusable C# classes using AgentClassSkill.
// Class-based skills bundle all components into a single class that can be shared as a library.

using Agent_Step04_ClassBasedSkills;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using OpenAI.Responses;

// --- Configuration ---
string endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT") ?? throw new InvalidOperationException("AZURE_OPENAI_ENDPOINT is not set.");
string deploymentName = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT_NAME") ?? "gpt-4o-mini";

// --- Class-Based Skill ---
// Instantiate the skill class.
var unitConverter = new UnitConverterSkill();

// --- Skills Provider ---
var skillsProvider = new AgentSkillsProviderBuilder()
    .AddClassSkills(unitConverter)
    .Build();

// --- Agent Setup ---
AIAgent agent = new AzureOpenAIClient(new Uri(endpoint), new DefaultAzureCredential())
    .GetResponsesClient(deploymentName)
    .AsAIAgent(new ChatClientAgentOptions
    {
        Name = "UnitConverterAgent",
        ChatOptions = new()
        {
            Instructions = "You are a helpful assistant that can convert units.",
        },
        AIContextProviders = [skillsProvider],
    });

// --- Example: Unit conversion ---
Console.WriteLine("Converting units with class-based skills");
Console.WriteLine(new string('-', 60));

AgentResponse response = await agent.RunAsync(
    "How many kilometers is a marathon (26.2 miles)? And how many pounds is 75 kilograms?");

Console.WriteLine($"Agent: {response.Text}");
