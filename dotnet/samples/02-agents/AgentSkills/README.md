# AgentSkills Samples

Samples demonstrating Agent Skills capabilities. Each sample shows a different way to define and use skills.

| Sample | Description |
|--------|-------------|
| [Agent_Step01_FileBasedSkills](Agent_Step01_FileBasedSkills/) | Define skills as `SKILL.md` files on disk with reference documents. Uses a unit-converter skill. |
| [Agent_Step02_CodeDefinedSkills](Agent_Step02_CodeDefinedSkills/) | Define skills entirely in C# code using `AgentInlineSkill`, with static/dynamic resources and scripts. |
| [Agent_Step03_ClassBasedSkills](Agent_Step03_ClassBasedSkills/) | Define skills as reusable C# classes using `AgentClassSkill`. |
| [Agent_Step04_MixedSkills](Agent_Step04_MixedSkills/) | Combine file-based, code-defined, and class-based skills in a single agent via `AgentSkillsProviderBuilder`. |
| [Agent_Step05_CodeDefinedSkillsWithDI](Agent_Step05_CodeDefinedSkillsWithDI/) | Use Dependency Injection with skill scripts — resolve services from `IServiceProvider` at execution time. |
| [Agent_Step06_ClassBasedSkillsWithDI](Agent_Step06_ClassBasedSkillsWithDI/) | Use Dependency Injection with class-based skills — resolve services from `IServiceProvider` in `AgentClassSkill` resources and scripts. |

## Key Concepts

### File-Based vs Code-Defined vs Class-Based Skills

| Aspect | File-Based | Code-Defined | Class-Based |
|--------|-----------|--------------|-------------|
| Definition | `SKILL.md` files on disk | `AgentInlineSkill` instances in C# | Classes extending `AgentClassSkill` |
| Resources | All files in skill directory (filtered by extension) | `AgentInlineSkillResource` (static value or delegate-backed) | `AgentInlineSkillResource` (static value or delegate-backed) |
| Scripts | Supported via script executor delegate | `AgentInlineSkillScript` delegates | `AgentInlineSkillScript` in class |
| Discovery | Automatic from directory path | Explicit via constructor | Explicit via constructor |
| Dynamic content | No (static files only) | Yes (factory delegates) | Yes (factory delegates) |
| Reusability | Copy skill directory | Inline or shared instances | Share as library class |

For single-source scenarios, use the `AgentSkillsProvider` constructors directly. To combine multiple skill types, use the `AgentSkillsProviderBuilder` — see [Agent_Step04_MixedSkills](Agent_Step04_MixedSkills/).

