// Copyright (c) Microsoft. All rights reserved.

// This sample demonstrates how to use Agent Skills with a ChatClientAgent.
// Agent Skills are modular packages of instructions, resources, and scripts that extend an agent's capabilities.
// The skills follow the progressive disclosure pattern: advertise → load → read resources → run scripts.

using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using OpenAI.Chat;

var endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT") ?? throw new InvalidOperationException("AZURE_OPENAI_ENDPOINT is not set.");
var deploymentName = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT_NAME") ?? "gpt-4o-mini";

// Configure logging to see what's happening with skill discovery and loading
using ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Information);
});

// Set up Agent Skills Provider
// Skills are discovered from the 'skills' directory and made available to the agent
var skillsProvider = new AgentSkillsProvider(
    new AgentSkillsProviderOptions
    {
        // Specify directories to search for skills
        SkillDirectories = [Path.Combine(Directory.GetCurrentDirectory(), "skills")],

        // Optional: Configure script execution timeout (default is 30 seconds)
        ScriptTimeoutMilliseconds = 30000
    },
    loggerFactory);

// Create an agent with the Agent Skills Provider
// The provider will:
// 1. Discover and validate skills from the skills directory
// 2. Advertise available skills to the agent via system prompt
// 3. Provide tools for the agent to load skills, read resources, and run scripts
// WARNING: DefaultAzureCredential is convenient for development but requires careful consideration in production.
// In production, consider using a specific credential (e.g., ManagedIdentityCredential) to avoid
// latency issues, unintended credential probing, and potential security risks from fallback mechanisms.
AIAgent agent = new AzureOpenAIClient(
    new Uri(endpoint),
    new DefaultAzureCredential())
    .GetChatClient(deploymentName)
    .AsAIAgent(new ChatClientAgentOptions
    {
        Name = "SkillsAgent",
        ChatOptions = new()
        {
            Instructions = "You are a helpful assistant."
        },
        // Attach the Agent Skills Provider
        // The provider will automatically advertise available skills and provide the tools to use them
        AIContextProviders = [skillsProvider]
    });

// Example 1: Simple question that should trigger skill usage
Console.WriteLine("Example 1: Using the calculator skill");
Console.WriteLine("───────────────────────────────────────");
AgentResponse response1 = await agent.RunAsync("Calculate 15% of 250");
Console.WriteLine($"Agent: {response1.Text}");
Console.WriteLine();

//// Example 2: Question requiring multiple skills
//Console.WriteLine("Example 2: Using multiple skills");
//Console.WriteLine("───────────────────────────────────────");
//AgentResponse response2 = await agent.RunAsync("What mathematical operations can you perform?");
//Console.WriteLine($"Agent: {response2.Text}");
//Console.WriteLine();

//// Example 3: Using skills with a session (maintains conversation context)
//Console.WriteLine("Example 3: Multi-turn conversation with skills");
//Console.WriteLine("───────────────────────────────────────");
//AgentSession session = await agent.CreateSessionAsync();
//AgentResponse response3 = await agent.RunAsync("What skills do you have available?", session);
//Console.WriteLine($"Agent: {response3.Text}");
//Console.WriteLine();

//AgentResponse response4 = await agent.RunAsync("Show me how to use the calculator skill", session);
//Console.WriteLine($"Agent: {response4.Text}");
//Console.WriteLine();

//// Example 4: Skill with resources
//Console.WriteLine("Example 4: Accessing skill resources");
//Console.WriteLine("───────────────────────────────────────");
//AgentResponse response5 = await agent.RunAsync("Can you load the web-search skill and tell me what additional documentation is available?", session);
//Console.WriteLine($"Agent: {response5.Text}");
//Console.WriteLine();
