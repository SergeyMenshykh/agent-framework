// Copyright (c) Microsoft. All rights reserved.

// This sample demonstrates how to use Dependency Injection (DI) with class-based Agent Skills.
// Unlike code-defined skills (Step06), class-based skills bundle all components into a single
// reusable class extending AgentClassSkill. Skill script and resource functions can still resolve
// services from the DI container via IServiceProvider, combining the reusability of class-based
// skills with the flexibility of DI.

using Agent_Step07_ClassBasedSkillsWithDI;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.DependencyInjection;
using OpenAI.Responses;

// --- Configuration ---
string endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT") ?? throw new InvalidOperationException("AZURE_OPENAI_ENDPOINT is not set.");
string deploymentName = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT_NAME") ?? "gpt-4o-mini";

// --- Class-Based Skill with DI ---
// Instantiate the skill class. Its resources and scripts will resolve services from
// the DI container at execution time.
var unitConverter = new UnitConverterSkill();

// --- Skills Provider ---
var skillsProvider = new AgentSkillsProvider(unitConverter);

// --- DI Container ---
// Register application services that skill scripts can resolve at execution time.
ServiceCollection services = new();
services.AddSingleton<ConversionRateService>();

// --- Agent Setup ---
AIAgent agent = new AzureOpenAIClient(new Uri(endpoint), new DefaultAzureCredential())
    .GetResponsesClient()
    .AsAIAgent(
        options: new ChatClientAgentOptions
        {
            Name = "UnitConverterAgent",
            ChatOptions = new()
            {
                Instructions = "You are a helpful assistant that can convert units.",
            },
            AIContextProviders = [skillsProvider],
        },
        model: deploymentName,
        services: services.BuildServiceProvider());

// --- Example: Unit conversion ---
Console.WriteLine("Converting units with DI-powered class-based skills");
Console.WriteLine(new string('-', 60));

AgentResponse response = await agent.RunAsync(
    "How many kilometers is a marathon (26.2 miles)? And how many pounds is 75 kilograms?");

Console.WriteLine($"Agent: {response.Text}");
