// Copyright (c) Microsoft. All rights reserved.

// This sample demonstrates how to use Dependency Injection (DI) with Agent Skills.
// Skill script functions can resolve services from the DI container via IServiceProvider,
// enabling clean separation of concerns and testability.
//
// The sample registers a ConversionRateService in the DI container. A code-defined skill
// script then resolves this service to look up live conversion rates at execution time,
// rather than relying on hardcoded factors.

using System.Text.Json;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.DependencyInjection;
using OpenAI.Responses;

// --- Configuration ---
string endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT") ?? throw new InvalidOperationException("AZURE_OPENAI_ENDPOINT is not set.");
string deploymentName = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT_NAME") ?? "gpt-4o-mini";

// --- Build the code-defined skill ---
// The skill uses DI to resolve ConversionRateService in its script function.
var unitConverterSkill = new AgentCodeSkill(
    name: "unit-converter",
    description: "Convert between common units. Use when asked to convert miles, kilometers, pounds, or kilograms.",
    instructions: """
        Use this skill when the user asks to convert between units.

        1. Use the convert script, passing the value and the source/target unit names.
           The script will look up the conversion rate from the registered rate service.
        """)
    // Static resource: lists which conversions are supported
    .AddResource(
        "supported-conversions",
        """
        # Supported Conversions

        | From        | To          |
        |-------------|-------------|
        | miles       | kilometers  |
        | kilometers  | miles       |
        | pounds      | kilograms   |
        | kilograms   | pounds      |
        """)
    // Script with DI: resolves ConversionRateService from the service provider
    .AddScript((double value, string fromUnit, string toUnit, IServiceProvider serviceProvider) =>
    {
        // Resolve the conversion rate service from the DI container
        var rateService = serviceProvider.GetRequiredService<ConversionRateService>();

        double factor = rateService.GetRate(fromUnit, toUnit);
        double result = Math.Round(value * factor, 4);

        return JsonSerializer.Serialize(new { value, fromUnit, toUnit, factor, result });
    }, "convert");

// --- Skills Provider ---
var skillsProvider = new AgentSkillsProviderBuilder()
    .AddCodeSkills(unitConverterSkill)
    .Build();

// --- DI Container ---
// Register application services that skill scripts can resolve at execution time.
ServiceCollection services = new();
services.AddSingleton<ConversionRateService>();

// --- Agent Setup ---
AIAgent agent = new AzureOpenAIClient(new Uri(endpoint), new DefaultAzureCredential())
    .GetResponsesClient(deploymentName)
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
        services: services.BuildServiceProvider());

// --- Example: Unit conversion ---
Console.WriteLine("Converting units with DI-powered skills");
Console.WriteLine(new string('-', 60));

AgentResponse response = await agent.RunAsync(
    "How many kilometers is a marathon (26.2 miles)? And how many pounds is 75 kilograms?");

Console.WriteLine($"Agent: {response.Text}");

// ---------------------------------------------------------------------------
// Services
// ---------------------------------------------------------------------------

/// <summary>
/// Provides conversion rates between units.
/// In a real application this could call an external API, read from a database,
/// or apply time-varying exchange rates.
/// </summary>
internal sealed class ConversionRateService
{
    private static readonly Dictionary<(string From, string To), double> s_rates = new()
    {
        [("MILES", "KILOMETERS")] = 1.60934,
        [("KILOMETERS", "MILES")] = 0.621371,
        [("POUNDS", "KILOGRAMS")] = 0.453592,
        [("KILOGRAMS", "POUNDS")] = 2.20462,
    };

    /// <summary>
    /// Gets the conversion factor for the specified unit pair.
    /// </summary>
    public double GetRate(string fromUnit, string toUnit)
    {
        var key = (fromUnit.ToUpperInvariant(), toUnit.ToUpperInvariant());
        if (s_rates.TryGetValue(key, out double rate))
        {
            return rate;
        }

        throw new ArgumentException($"No conversion rate found for {fromUnit} → {toUnit}.");
    }
}
