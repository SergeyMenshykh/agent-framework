# Mixed Agent Skills Sample

This sample demonstrates how to **combine multiple skill types** in a single agent using the `AgentSkillsProviderBuilder`.

## What it demonstrates

- Combining file-based, code-defined, and class-based skills in one provider
- Using `UseFileSkill`, `UseInlineSkills`, and `UseClassSkills` on the builder
- Aggregating skills from all sources into a single provider

## Skills Included

### unit-converter (file-based)

Discovered from `skills/unit-converter/SKILL.md` on disk. Converts miles↔km, pounds↔kg.

### volume-converter (code-defined)

Defined as `AgentInlineSkill` in `Program.cs`. Converts gallons↔liters.

### temperature-converter (class-based)

Defined as `TemperatureConverterSkill` class in `TemperatureConverterSkill.cs`. Converts °F↔°C↔K.

## Running the Sample

### Prerequisites

- .NET 10.0 SDK
- Azure OpenAI endpoint with a deployed model

### Setup

```bash
export AZURE_OPENAI_ENDPOINT="https://your-endpoint.openai.azure.com/"
export AZURE_OPENAI_DEPLOYMENT_NAME="gpt-4o-mini"
```

### Run

```bash
dotnet run
```

### Expected Output

```
Converting with mixed skills (file + code + class)
------------------------------------------------------------
Agent: Here are your conversions:

1. **26.2 miles → 42.16 km** (a marathon distance)
2. **5 gallons → 18.93 liters**
3. **98.6°F → 37.0°C**
```
