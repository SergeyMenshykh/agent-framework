# Agent Skills Sample

This sample demonstrates how to use **Agent Skills** with a `ChatClientAgent` in the Microsoft Agent Framework.

## What are Agent Skills?

Agent Skills are modular packages of instructions, scripts, and resources that enable AI agents to perform specialized tasks. They follow the [Agent Skills specification](https://agentskills.io/) and implement the progressive disclosure pattern:

1. **Advertise**: Skills are advertised with name + description (~100 tokens per skill)
2. **Load**: Full instructions are loaded on-demand via `load_skill` tool (<5000 tokens)
3. **Resources**: Supplementary files loaded via `read_skill_resource` tool (as needed)
4. **Scripts**: Executable scripts invoked via `run_skill_script` tool (as needed)

## Sample Overview

This sample shows:
- How to create and structure skills in a `skills/` directory
- How to register the `AgentSkillsProvider` with an agent
- How the agent discovers and uses skills progressively
- How to create skills with resources and executable scripts

## Project Structure

```
Agent_Step21_UsingAgentSkills/
├── Program.cs                      # Main sample code
├── Agent_Step21_UsingAgentSkills.csproj
└── skills/                         # Skills directory
    ├── web-search/                 # Web search skill
    │   ├── SKILL.md               # Skill definition with YAML frontmatter
    │   └── REFERENCE.md           # Additional documentation
    └── calculator/                 # Calculator skill
        ├── SKILL.md               # Skill definition
        └── scripts/
            └── calculate          # Executable script (PowerShell/bash)
```

## SKILL.md Format

Each skill is defined in a `SKILL.md` file with YAML frontmatter:

```markdown
---
name: skill-name
description: Brief description of what the skill does
resources:
  - REFERENCE.md
  - FORMS.md
scripts:
  - script_name
compatibility: Optional compatibility info
---

# Skill Instructions

Full markdown instructions go here...
```

## How Skills Work

### 1. Skill Discovery
The `AgentSkillsProvider` scans directories for `SKILL.md` files:

```csharp
var skillsProvider = new AgentSkillsProvider(
    new AgentSkillsProviderOptions
    {
        SkillDirectories = [Path.Combine(Directory.GetCurrentDirectory(), "skills")]
    },
    loggerFactory);
```

### 2. Skill Validation
At initialization, the provider validates that:
- All resources listed in frontmatter exist as files
- All scripts listed in frontmatter exist and are accessible
- Invalid skills are excluded with warnings logged

### 3. Skill Registration
Attach the provider to an agent's `AIContextProviders`:

```csharp
AIAgent agent = chatClient.AsAIAgent(new ChatClientAgentOptions
{
    AIContextProviders = [skillsProvider]
});
```

### 4. Progressive Loading
The agent sees:
- **Initially**: List of available skills (name + description only)
- **On demand**: Full skill instructions via `load_skill("skill-name")`
- **As needed**: Resources via `read_skill_resource("skill-name", "REFERENCE.md")`
- **When required**: Script execution via `run_skill_script("skill-name", "script", args)`

## Running the Sample

### Prerequisites
- .NET 10.0 SDK
- Azure OpenAI endpoint and API key

### Setup
1. Set environment variables:
   ```bash
   export AZURE_OPENAI_ENDPOINT="https://your-endpoint.openai.azure.com/"
   export AZURE_OPENAI_DEPLOYMENT_NAME="gpt-4o-mini"
   ```

2. Run the sample:
   ```bash
   dotnet run
   ```

### Expected Output
The sample demonstrates:
1. Using the calculator skill for mathematical operations
2. Querying available skills
3. Multi-turn conversations with skill context
4. Loading skill resources for additional information

## Creating Your Own Skills

### Minimal Skill
```
my-skill/
└── SKILL.md
```

### Skill with Resources
```
my-skill/
├── SKILL.md
├── REFERENCE.md
└── EXAMPLES.md
```

### Skill with Scripts
```
my-skill/
├── SKILL.md
└── scripts/
    ├── process.ps1    # For Windows
    └── process.sh     # For Linux/Mac
```

## Key Features

- ✅ **Automatic Discovery**: Skills are found and loaded from directories
- ✅ **Progressive Disclosure**: Minimizes token usage
- ✅ **Validation**: Ensures all referenced files exist
- ✅ **Cross-platform Scripts**: PowerShell (Windows) and bash (Linux/Mac)
- ✅ **Multi-skill Support**: Use multiple skills in one conversation
- ✅ **Resource Loading**: Access supplementary documentation on-demand
- ✅ **Script Execution**: Run external scripts with timeout and output capture

## Learn More

- [Agent Skills Specification](https://agentskills.io/)
- [Microsoft Agent Framework Documentation](../../../../docs/)
- [AIContextProvider Pattern](../Agent_Step20_AdditionalAIContext/)
