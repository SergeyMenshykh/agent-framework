// Copyright (c) Microsoft. All rights reserved.

// This sample demonstrates how to use Agent Skills with a ChatClientAgent.
// Agent Skills are modular packages of instructions, scripts, and resources that extend an agent's capabilities.
// Skills follow the progressive disclosure pattern: advertise -> load -> read resources -> execute scripts.
//
// This sample includes two skills:
//   - expense-report: Policy-based expense filing with references and assets
//   - password-generator: Secure credential generation using approved Python scripts

using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI.Responses;

// --- Configuration ---
string endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT")
    ?? throw new InvalidOperationException("AZURE_OPENAI_ENDPOINT is not set.");
string deploymentName = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT_NAME") ?? "gpt-4o-mini";

// --- Skills Provider ---
// Discovers skills from the 'skills' directory and makes them available to the agent
var skillsProvider = new FileAgentSkillsProvider(skillPath: Path.Combine(Directory.GetCurrentDirectory(), "skills"));

// --- Agent Setup ---
AIAgent agent = new AzureOpenAIClient(new Uri(endpoint), new DefaultAzureCredential())
    .GetResponsesClient(deploymentName)
    .AsAIAgent(new ChatClientAgentOptions
    {
        Name = "SkillsAgent",
        ChatOptions = new()
        {
            Instructions = "You are a helpful assistant.",
            // Code interpreter enables the agent to execute Python scripts from skills
            Tools = [new HostedCodeInterpreterTool()]
        },
        AIContextProviders = [skillsProvider],
    });

// --- Example 1: Expense policy question (loads FAQ resource) ---
Console.WriteLine("Example 1: Checking expense policy FAQ");
Console.WriteLine("---------------------------------------");
AgentResponse response1 = await agent.RunAsync("Are tips reimbursable? I left a 25% tip on a taxi ride and want to know if that's covered.");
Console.WriteLine($"Agent: {response1.Text}\n");

// --- Example 2: Filing an expense report (multi-turn with template asset) ---
Console.WriteLine("Example 2: Filing an expense report");
Console.WriteLine("---------------------------------------");
AgentSession session = await agent.CreateSessionAsync();
AgentResponse response2 = await agent.RunAsync("I had 3 client dinners and a $1,200 flight last week. Return a draft expense report and ask about any missing details.",
    session);
Console.WriteLine($"Agent: {response2.Text}\n");

// --- Example 3: Generate a password (reads strength guide, then executes password.py) ---
Console.WriteLine("Example 3: Generating a password");
Console.WriteLine("---------------------------------------");
AgentResponse response3 = await agent.RunAsync("I need a new password for my company's production database admin account.");
Console.WriteLine($"Agent: {response3.Text}\n");

// --- Example 4: Generate a PIN (reads strength guide, then executes pin.py) ---
Console.WriteLine("Example 4: Generating a PIN");
Console.WriteLine("---------------------------------------");
AgentResponse response4 = await agent.RunAsync("I need a new PIN for my new device.");
Console.WriteLine($"Agent: {response4.Text}\n");
