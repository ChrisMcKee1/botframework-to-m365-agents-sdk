# What does NOT migrate

Source: https://learn.microsoft.com/microsoft-365/agents-sdk/bf-migration-guidance (§ Unsupported and deprecated packages). Verified 2026-05-14.

This is the most important list in the playbook. If the existing bot depends on anything in this table, it's not a package swap — it's a refactor / rebuild for that subsystem.

## Full table of unsupported features

| Bot Framework feature | Status in Agents SDK | Replacement |
|---|---|---|
| **Adaptive Dialogs** | Not supported. Microsoft does not plan to bring them forward. | Rebuild flow as code (turn handlers, dialogs under `Microsoft.Agents.Builder.Dialogs`, or an AI-orchestrated approach). |
| **AdaptiveExpressions** (`Microsoft.Bot.AdaptiveExpressions.Core`) | Packages still publish from BotBuilder side and can be used at your own risk, but **not actively supported**. They don't take dependencies on Agents SDK packages. | Replace with code / templates / LLM-generated content. |
| **Bot Framework Composer artifacts** | Not supported. | Anything authored in Composer (adaptive dialogs, LG, etc.) must be rebuilt. |
| **Bot Framework Composer (tool)** | Not supported. Agents Toolkit replaces it as the authoring surface. | Microsoft 365 Agents Toolkit. |
| **Language Generation (LG)** | Tooling, templates, parsers not needed. | General-purpose LLMs. |
| **Language Understanding (LUIS)** + `Microsoft.Bot.Builder.Parsers.LU` | Not supported. Online service already disabled. | Azure OpenAI + retrieval (Foundry / Microsoft Agent Framework). |
| **Orchestrator** | Not needed. | Modern LLM orchestration. |
| **QnA Maker** | Not supported. Online service already disabled. | Azure OpenAI + retrieval (Foundry / Microsoft Agent Framework). |
| **TemplateManager** | Not supported. | LLMs generate dynamic responses. |
| **`BotFrameworkAdapter`** | Removed. | `CloudAdapter` (the only adapter in the SDK). |
| **Bot Framework CLI (`bf`)** | Deprecated. All commands replaced. | Microsoft 365 Agents Toolkit + its CLI. |
| **Generators** (Yeoman etc.) | Not brought forward. | Agents Toolkit templates (VS / VS Code / CLI). |
| **ASP.NET WebAPI** | Not supported in C# samples. | ASP.NET Core (a.k.a. ASP.NET Core Web API) with minimal APIs. |
| **Application Insights bot-telemetry helpers** | Legacy approach replaced. | Modern cloud-native observability (OpenTelemetry, standard logging). |
| **Inspection** (legacy debugging/inspection middleware) | Not supported. | Agents Toolkit + Playground debugging. |
| **Streaming Connections** (legacy implementation) | Not compatible. | Redesigned streaming for modern AI patterns (see `StreamingResponse` in Copilot streaming guidance). |
| **QueueStorage** in BotBuilder | Replaced. | Cloud-native messaging patterns. |
| **Deprecated Activities** (e.g., payments activities) | Removed. | N/A — they don't match modern agent patterns. |

## Compatibility surfaces that DO carry over

These are not gone — they are explicitly preserved as bridges:

| Feature | Bridge package |
|---|---|
| Dialogs (waterfall, component) | `Microsoft.Agents.Builder.Dialogs` (.NET) / `botbuilder-dialogs` → `@microsoft/agents-hosting-dialogs` (Node) |
| `ActivityHandler` base class | Available in .NET, JS, and Python under the new namespaces; **JS marks it deprecated in favor of `AgentApplication`** |
| `ConversationState`, `UserState`, `PrivateConversationState` | Same API shape, new namespace (`Microsoft.Agents.Builder.State` / `microsoft_agents.hosting.core`) |
| Teams extensions | `Microsoft.Agents.Extensions.Teams` + `Microsoft.Agents.Extensions.Teams.Compat` (.NET); `microsoft-agents-hosting-teams` (Python); `@microsoft/agents-hosting-extensions-teams` (Node) |
| `IStorage` + Blob / Cosmos providers | `Microsoft.Agents.Storage.*` namespaces |
| Middleware (`IMiddleware`) | Still registerable via DI; prefer new turn lifecycle hooks for new code |

## Scope-sizing implication

Three questions decide scope:

1. **Composer / Adaptive Dialogs / LG / Adaptive Expressions?** → if yes, refactor budget.
2. **LUIS / QnA Maker?** → if yes, AI-replacement budget (Foundry / Microsoft Agent Framework + retrieval).
3. **App Insights bot-telemetry helpers?** → if yes, observability refactor (small, but mention it).

Everything else is a package + namespace + config delta covered in the per-language deep-dives.
