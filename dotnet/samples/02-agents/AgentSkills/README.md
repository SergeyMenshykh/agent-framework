# AgentSkills Samples

Samples demonstrating Agent Skills capabilities. Each sample shows a different way to define and use skills.

| Sample | Description |
|--------|-------------|
| [Agent_Step01_BasicSkills](Agent_Step01_BasicSkills/) | Using Agent Skills with a ChatClientAgent, including progressive disclosure and skill resources |
| [Agent_Step02_FileBasedSkills](Agent_Step02_FileBasedSkills/) | Define skills as `SKILL.md` files on disk with reference documents. Uses a unit-converter skill. |
| [Agent_Step03_CodeDefinedSkills](Agent_Step03_CodeDefinedSkills/) | Define skills entirely in C# code using `AgentCodeSkill`, with static/dynamic resources and scripts. |
| [Agent_Step04_ClassBasedSkills](Agent_Step04_ClassBasedSkills/) | Define skills as reusable C# classes using `AgentClassSkill`. |
| [Agent_Step05_MixedSkills](Agent_Step05_MixedSkills/) | Combine file-based, code-defined, and class-based skills in a single agent via `AgentSkillsProviderBuilder`. |
| [Agent_Step06_CodeDefinedSkillsWithDI](Agent_Step06_CodeDefinedSkillsWithDI/) | Use Dependency Injection with skill scripts — resolve services from `IServiceProvider` at execution time. |
| [Agent_Step07_ClassBasedSkillsWithDI](Agent_Step07_ClassBasedSkillsWithDI/) | Use Dependency Injection with class-based skills — resolve services from `IServiceProvider` in `AgentClassSkill` resources and scripts. |

## Key Concepts

### File-Based vs Code-Defined vs Class-Based Skills

| Aspect | File-Based | Code-Defined | Class-Based |
|--------|-----------|--------------|-------------|
| Definition | `SKILL.md` files on disk | `AgentCodeSkill` instances in C# | Classes extending `AgentClassSkill` |
| Resources | Static files in `references/` and `assets/` | `AgentCodeSkillResource` (static or dynamic) | `AgentCodeSkillResource` in class |
| Scripts | Not yet supported in .NET | `AgentCodeSkillScript` delegates | `AgentCodeSkillScript` in class |
| Discovery | Automatic via `AddFileSource` | Explicit via `AddCodeSkills` | Explicit via `AddClassSkills` |
| Dynamic content | No (static files only) | Yes (factory delegates) | Yes (factory delegates) |
| Reusability | Copy skill directory | Inline or shared instances | Share as library class |

All types can be combined in a single `AgentSkillsProviderBuilder` — see [Agent_Step05_MixedSkills](Agent_Step05_MixedSkills/).

