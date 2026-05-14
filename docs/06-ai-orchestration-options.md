# 06 — AI orchestration options

Backing notes: [`../research/10-ai-orchestration-options.md`](../research/10-ai-orchestration-options.md). Citations: [`../research/sources.md`](../research/sources.md).

## The SDK is AI-agnostic

The Agents SDK does **not** pick a model or orchestrator. It hands you `Activity`, `TurnContext`, and state — you wire in the AI.

That's a feature: a team can migrate **off** Bot Framework first, then make AI decisions on their own timeline.

## Microsoft's recommended orchestrator: **Microsoft Agent Framework**

**Microsoft Agent Framework** is the direct successor to **Semantic Kernel** and **AutoGen**, built by the same teams. Per MS Learn:

> Agent Framework combines AutoGen's simple agent abstractions with Semantic Kernel's enterprise features — session-based state management, type safety, middleware, telemetry — and adds graph-based workflows for explicit multi-agent orchestration. In short, Agent Framework is the next generation of both Semantic Kernel and AutoGen.

For new work in the Agents SDK, **start with Agent Framework**. The MS Learn doc [*Use Semantic Kernel and Agent Framework in Agents SDK*](https://learn.microsoft.com/microsoft-365/agents-sdk/using-semantic-kernel-agent-framework) walks both, and explicitly positions Agent Framework as the modern path.

| Surface | Semantic Kernel | Microsoft Agent Framework |
|---|---|---|
| Namespace (.NET) | `Microsoft.SemanticKernel`, `Microsoft.SemanticKernel.Agents` | `Microsoft.Agents.AI` (uses `Microsoft.Extensions.AI` for messages/content) |
| Core abstraction | `Kernel` + service-specific agent types (`ChatCompletionAgent`, `OpenAIAssistantAgent`, `AzureAIAgent`) | `IChatClient` + unified `ChatClientAgent` (base type: `AIAgent`) |
| DI registration | `services.AddKernel().AddProvider(...)` then keyed `Agent` singleton | `services.AddSingleton<IChatClient>(...)` then `ChatClientAgent` |
| Tools | `[KernelFunction, Description("…")]` on a plugin class | `[Description("…")]` on a method, register via `AIFunctionFactory.Create(...)` on `ChatOptions.Tools` |
| Invocation | `agent.InvokeAsync` / `InvokeStreamingAsync` returns `StreamingChatMessageContent` | `agent.RunAsync` / `RunStreamingAsync` returns `AgentResponseUpdate` |
| Options | `OpenAIPromptExecutionSettings` + `AgentInvokeOptions` | `ChatClientAgentRunOptions(new ChatOptions { MaxOutputTokens = ... })` |

Status (verified May 2026):
- **C# core** is released (e.g., `Microsoft.Agents.AI.Abstractions` in-memory chat history provider, `TextSearchProvider`). Several integrations are still **Preview**.
- **Python** is **public preview** in the Agents SDK orchestration context.

Migration path from SK to Agent Framework: [*Semantic Kernel to Agent Framework Migration Guide*](https://learn.microsoft.com/agent-framework/migration-guide/from-semantic-kernel/).

## Five orchestration options

| Option | What | When |
|---|---|---|
| **Microsoft Agent Framework** | Microsoft's open-source successor to Semantic Kernel + AutoGen. C# (released core) and Python (preview). Unified `IChatClient` / `ChatClientAgent`. | **Default recommendation** for pro-code .NET / Python teams. |
| **Azure AI Foundry** | Hosted agent service + model catalog. Bring an existing Foundry agent into your Agents-SDK app. | Already have / want a managed model + agent runtime in Azure. Can be combined with Agent Framework. |
| **Semantic Kernel** | The predecessor framework. Still supported; receives an official migration guide to Agent Framework. | Existing SK codebase you can't migrate yet. |
| **OpenAI Agents** | OpenAI Assistants / Agents API. | Already standardized on OpenAI directly. |
| **LangChain** | Third-party open-source orchestration. | Already invested in LangChain (often Python). |
| **Custom / direct** | Direct LLM API calls + your own logic. | You want the framework out of the way. |

The Microsoft 365 Agents Toolkit ships templates (e.g., the Weather Agent) with Foundry pre-wired. Some legacy templates may still scaffold Semantic Kernel — swap for Agent Framework per the migration guide above for new work.

## Replacing LUIS / QnA Maker — the recipe

LUIS and QnA Maker are **retired services**. The MS Learn migration guidance is explicit:

> LUIS and QnA Maker are retired, so replace any remaining usage with supported approaches such as Azure OpenAI with retrieval.

Replacement formula:

| Old | New |
|---|---|
| LUIS intent classification | LLM (Azure OpenAI) with system prompt and function calling |
| LUIS entity extraction | LLM with structured outputs / function calling |
| QnA Maker FAQ | Azure AI Search + RAG, or knowledge sources on a Foundry agent |
| Orchestrator (legacy multi-model router) | Foundry orchestration or **Agent Framework** workflows |

Three building blocks: **Azure OpenAI** (the model) + **retrieval** (Azure AI Search / Microsoft Graph / your own vector store) + **orchestration** (Foundry agents *or* Microsoft Agent Framework *or* LangChain).

## Decision matrix

| Signal | Recommended path |
|---|---|
| Pure pro-code .NET team, want Microsoft-owned stack end-to-end | **Microsoft Agent Framework** + Azure OpenAI / Foundry |
| Already operating Foundry agents | **Foundry agent integration** (portal or via Agents Toolkit proxy) — orchestrate from Agent Framework |
| Multi-step agentic workflows, tools / function calling primary | **Microsoft Agent Framework** (workflows, function calling) or **OpenAI Agents** |
| Python team already on LangChain | **LangChain** + Azure OpenAI |
| RAG-heavy, lots of documents to ground on | **Azure AI Search** + Agent Framework / Foundry retrieval |
| Replacing LUIS specifically | LLM with structured outputs + small Agent Framework orchestrator |
| Replacing QnA Maker specifically | Azure AI Search + RAG, or Foundry agent with knowledge sources |
| Existing Semantic Kernel code, can't migrate yet | Keep on **Semantic Kernel**, plan migration to Agent Framework using the official migration guide |

## Two ways to integrate a Foundry agent

| Path | Tooling | Best for |
|---|---|---|
| **Publish from Foundry portal** | Foundry portal auto-provisions Azure Bot + Entra app | Rapid deployment, minimal code changes. |
| **Proxy via Agents Toolkit** | VS Code / VS + Agents Toolkit | Advanced customization, SSO, managed infra, multi-environment. |

If your team already runs logic in Foundry, the **proxy pattern is Microsoft-recommended** — but only as a phase-2 step, not the migration baseline.

## Knowledge access (the data the agent grounds on)

- **Copilot Studio agents** get native access to Microsoft 365 + Copilot connectors.
- **Pro-code agents** (Agents SDK / Foundry via Toolkit) access the same data via:
  - Microsoft Graph APIs
  - The **Retrieval API** for grounding in Microsoft 365 data
  - Whatever vector store / search index you set up (Azure AI Search recommended)

The SDK does not ship a knowledge layer. Wire in Azure AI Search (or equivalent) yourself. Agent Framework provides RAG context providers (Text Search, Azure AI Search, Neo4j GraphRAG) for this.

## Stretch sample (gated)

A second `samples/after-agents-sdk-with-foundry/` is gated on whether your team's existing bot currently uses LUIS / QnA. If built, it should wire **Foundry + Microsoft Agent Framework** (not SK) per current MS Learn guidance. Confirm scope before building.

## Out of scope for v1

- Multi-agent orchestration (one agent calling another) — Agent Framework workflows cover this; separate doc.
- Agent-to-agent OAuth / federated identity (Entra Agent Identity Blueprints).

## Next

→ [`07-discovery-checklist.md`](./07-discovery-checklist.md)
