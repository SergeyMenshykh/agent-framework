// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using A2A;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.A2A;
using Microsoft.Extensions.AI;

namespace AgentConformance.IntegrationTests;

public class A2AAgentTests
{
    //[Fact]
    //public async Task RunAsync_WhenHandlingTask_ReturnsInitialResponseAsync()
    //{
    //    // Arrange
    //    AIAgent agent = await this.CreateA2AAgentAsync();

    //    // Act
    //    AgentRunResponse response = await agent.RunAsync("What is the capital of France?");

    //    // Assert
    //    Assert.NotNull(response);
    //    Assert.NotNull(response.ResponseId);
    //    Assert.NotNull(response.ContinuationToken);
    //}

    //[Fact]
    //public async Task RunAsync_HavingReturnedInitialResponse_AllowsCallerToPollAsync()
    //{
    //    // Part 1: Start the background run.
    //    AIAgent agent = await this.CreateA2AAgentAsync();

    //    AgentRunResponse response = await agent.RunAsync("What is the capital of France?");

    //    Assert.NotNull(response);
    //    Assert.NotNull(response.ResponseId);
    //    Assert.NotNull(response.ContinuationToken);

    //    // Part 2: Poll for completion.
    //    AgentRunOptions options = new();

    //    int attempts = 0;

    //    while (response.ContinuationToken is { } token && ++attempts < 10)
    //    {
    //        options.ContinuationToken = token;

    //        response = await agent.RunAsync([], options: options);

    //        // Wait for the response to be processed
    //        await Task.Delay(1000);
    //    }

    //    Assert.NotNull(response);
    //    Assert.Contains("Paris", response.Text);
    //    Assert.Null(response.ContinuationToken);
    //}

    //[Fact]
    //public async Task RunAsync_HavingFinishedTask_RefinesItsOutputAsync()
    //{
    //    string? originalContextId;
    //    string? originalTaskId;

    //    // Part 1: Start and finish the background run.
    //    AIAgent agent = await this.CreateA2AAgentAsync();

    //    AgentThread thread = agent.GetNewThread();

    //    AgentRunResponse response = await agent.RunAsync("What is the capital of France?", thread);
    //    Assert.NotNull(response.ContinuationToken);
    //    A2AAgentThread a2aThread = (A2AAgentThread)thread;
    //    Assert.NotNull(originalContextId = a2aThread.ContextId);
    //    Assert.NotNull(originalTaskId = a2aThread.TaskId);

    //    while (response.ContinuationToken is { } token)
    //    {
    //        response = await agent.RunAsync(thread, options: new AgentRunOptions { ContinuationToken = token });
    //        await Task.Delay(500);
    //    }
    //    Assert.Equal(originalContextId, a2aThread.ContextId);
    //    Assert.Equal(originalTaskId, a2aThread.TaskId);

    //    // Part 2: Refine the output of the previous task.
    //    response = await agent.RunAsync("Sorry I meant Belgium.", thread);
    //    Assert.NotNull(response.ContinuationToken);
    //    Assert.NotNull(a2aThread.ContextId);
    //    Assert.NotNull(a2aThread.TaskId);
    //    Assert.Equal(originalContextId, a2aThread.ContextId);
    //    Assert.NotEqual(originalTaskId, a2aThread.TaskId);

    //    while (response.ContinuationToken is { } token)
    //    {
    //        response = await agent.RunAsync(thread, options: new AgentRunOptions { ContinuationToken = token });
    //        await Task.Delay(500);
    //    }
    //    Assert.NotNull(a2aThread.ContextId);
    //    Assert.NotNull(a2aThread.TaskId);
    //    Assert.Equal(originalContextId, a2aThread.ContextId);
    //    Assert.NotEqual(originalTaskId, a2aThread.TaskId);
    //    Assert.Contains("Brussels", response.Text);
    //}

    [Fact]
    public async Task RunAsync_HavingTaskRequiringUserInput_CanHandleItAsync()
    {
        string? originalContextId;
        string? originalTaskId;

        AIAgent agent = await this.CreateA2AAgentAsync();

        AgentThread thread = agent.GetNewThread();

        // 1. Ask the agent to book a flight intentionally omitting details to trigger user input request
        AgentRunResponse response = await agent.RunAsync("I'd like to book a flight.", thread);
        Assert.NotNull(response.ContinuationToken);
        A2AAgentThread a2aThread = (A2AAgentThread)thread;
        Assert.NotNull(originalContextId = a2aThread.ContextId);
        Assert.NotNull(originalTaskId = a2aThread.TaskId);

        // 2. Poll for completion or user input request
        while (response.ContinuationToken is { } token)
        {
            response = await agent.RunAsync([], thread, options: new AgentRunOptions { ContinuationToken = token });
            await Task.Delay(500);
        }
        Assert.Equal(originalContextId, a2aThread.ContextId);
        Assert.Equal(originalTaskId, a2aThread.TaskId);

        // 3. Handle user input requests
        if (response.UserInputRequests.Any())
        {
            List<ChatMessage> messages = [];

            foreach (var requestContent in response.UserInputRequests.OfType<TextInputRequestContent>())
            {
                Assert.Contains("Where would you like to fly to, and from where?", requestContent.Request);
                messages.Add(new ChatMessage(ChatRole.User, [requestContent.CreateResponse("I want to fly from New York (JFK) to London (LHR) around October 10th, returning October 17th.")]));
            }

            response = await agent.RunAsync(messages, thread);
        }
        Assert.Equal(originalContextId, a2aThread.ContextId);
        Assert.Equal(originalTaskId, a2aThread.TaskId);

        // 4. Poll for completion.
        while (response.ContinuationToken is { } token)
        {
            response = await agent.RunAsync([], options: new AgentRunOptions { ContinuationToken = token });
            await Task.Delay(500);
        }
        Assert.Equal(originalContextId, a2aThread.ContextId);
        Assert.Equal(originalTaskId, a2aThread.TaskId);

        var dataContent = Assert.Single(response.Messages.SelectMany(m => m.Contents.OfType<DataContent>()));

        var originalContent = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(Encoding.UTF8.GetString(dataContent.Data.ToArray()));
        Assert.NotNull(originalContent);
        Assert.Equal("XYZ123", originalContent["confirmationId"].GetString());
        Assert.Equal("JFK", originalContent["from"].GetString());
        Assert.Equal("LHR", originalContent["to"].GetString());
        Assert.Equal("2024-10-10T18:00:00Z", originalContent["departure"].GetString());
        Assert.Equal("2024-10-11T06:00:00Z", originalContent["arrival"].GetString());
    }

    private async Task<AIAgent> CreateA2AAgentAsync()
    {
        A2ACardResolver a2ACardResolver = new(new Uri("http://localhost:5048"));

        return await a2ACardResolver.GetAIAgentAsync();
    }
}
