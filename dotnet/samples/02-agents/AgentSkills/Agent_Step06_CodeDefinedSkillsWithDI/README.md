# Agent Skills with Dependency Injection

This sample demonstrates how to use **Dependency Injection (DI)** with Agent Skills script functions.

## What It Shows

- Registering application services in a `ServiceCollection`
- Defining a code-based skill script that resolves services from `IServiceProvider`
- Passing the built `IServiceProvider` to the agent so skills can access DI services at execution time

## How It Works

1. A `ConversionRateService` is registered as a singleton in the DI container
2. A code-defined skill script declares `IServiceProvider` as a parameter — the framework injects it automatically
3. The script resolves `ConversionRateService` from the provider to look up conversion factors
4. The agent is created with the service provider, which flows through to skill script execution

## Prerequisites

- .NET 10
- An Azure OpenAI deployment

## Configuration

Set the following environment variables:

| Variable | Description |
|---|---|
| `AZURE_OPENAI_ENDPOINT` | Your Azure OpenAI endpoint URL |
| `AZURE_OPENAI_DEPLOYMENT_NAME` | Model deployment name (defaults to `gpt-4o-mini`) |

## Running the Sample

```bash
dotnet run
```
