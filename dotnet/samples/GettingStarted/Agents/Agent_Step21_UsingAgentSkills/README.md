# Agent Skills Sample

This sample demonstrates how to use **Agent Skills** with a `ChatClientAgent` in the Microsoft Agent Framework.

## What are Agent Skills?

Agent Skills are modular packages of instructions, scripts, and resources that enable AI agents to perform specialized tasks. They follow the [Agent Skills specification](https://agentskills.io/) and implement the progressive disclosure pattern:

1. **Advertise**: Skills are advertised with name + description (~100 tokens per skill)
2. **Load**: Full instructions are loaded on-demand via `load_skill` tool
3. **Resources**: Scripts, references, and other files loaded via `read_skill_resource` tool

> **Note:** Skills that include executable scripts (in the `scripts/` directory) require a code interpreter. The agent reads the script content via `read_skill_resource`, adjusts configuration as needed, and executes it through the code interpreter.

## Skills Included

### expense-report
Policy-based expense filing with spending limits, receipt requirements, and approval workflows.
- `references/POLICY_FAQ.md` — Detailed expense policy Q&A
- `assets/expense-report-template.md` — Submission template

### password-generator
Secure password and PIN generation using approved Python scripts executed via code interpreter.
- `scripts/password.py` — Generates random passwords with configurable character sets
- `scripts/pin.py` — Generates numeric PINs
- `references/PASSWORD_STRENGTH.md` — Password strength requirements
- `references/PIN_STRENGTH.md` — PIN strength requirements

## Project Structure

```
Agent_Step21_UsingAgentSkills/
├── Program.cs
├── Agent_Step21_UsingAgentSkills.csproj
└── skills/
    ├── expense-report/
    │   ├── SKILL.md
    │   ├── references/
    │   │   └── POLICY_FAQ.md
    │   └── assets/
    │       └── expense-report-template.md
    └── password-generator/
        ├── SKILL.md
        ├── scripts/
        │   ├── password.py
        │   └── pin.py
        └── references/
            ├── PASSWORD_STRENGTH.md
            └── PIN_STRENGTH.md
```

## Running the Sample

### Prerequisites
- .NET 10.0 SDK
- Azure OpenAI endpoint with a deployed model

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

### Examples

The sample runs four examples:

1. **Expense policy FAQ** — Asks about tip reimbursement; the agent loads the expense-report skill and reads the FAQ resource
2. **Filing an expense report** — Multi-turn conversation to draft an expense report using the template asset
3. **Generating a password** — Asks for a production database admin password; the agent reads the strength guide and executes `password.py` via code interpreter
4. **Generating a PIN** — Asks for a device PIN; the agent reads the PIN strength guide and executes `pin.py` via code interpreter

## Learn More

- [Agent Skills Specification](https://agentskills.io/)
- [Microsoft Agent Framework Documentation](../../../../docs/)
- [AIContextProvider Pattern](../Agent_Step20_AdditionalAIContext/)
