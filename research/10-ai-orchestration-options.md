# AI orchestration options for the "after" agent

Sources:
- https://learn.microsoft.com/microsoft-365/agents-sdk/using-semantic-kernel-agent-framework (canonical — covers both SK and Agent Framework wiring inside an Agents SDK app)
- https://learn.microsoft.com/agent-framework/overview/ (what Agent Framework is, status)
- https://learn.microsoft.com/agent-framework/migration-guide/from-semantic-kernel/ (SK → Agent Framework migration)
- https://learn.microsoft.com/microsoft-365/copilot/extensibility/create-deploy-agents-sdk
- https://learn.microsoft.com/microsoft-365/copilot/extensibility/m365-agents-sdk
- https://learn.microsoft.com/microsoft-365/copilot/extensibility/overview-custom-engine-agent
- https://learn.microsoft.com/microsoft-365/agents-sdk/bf-migration-guidance (§ unsupported — LUIS / QnA)

The Agents SDK is **AI-agnostic by design**. It hands you `Activity`, `TurnContext`, and state — it does not pick the model or the orchestrator. You wire those in.

## Microsoft's recommended orchestrator: **Microsoft Agent Framework**

Microsoft Agent Framework is the **direct successor** to both Semantic Kernel and AutoGen, created by the same teams. From the Agent Framework overview page:

> Agent Framework combines AutoGen's simple agent abstractions with Semantic Kernel's enterprise features — session-based state management, type safety, middleware, telemetry — and adds graph-based workflows for explicit multi-agent orchestration. … In short, Agent Framework is the next generation of both Semantic Kernel and AutoGen.

The dedicated MS Learn doc *Use Semantic Kernel and Agent Framework in Agents SDK* covers both paths inside an Agents SDK app. It walks the SK pattern first (because lots of code is on it today), then shows the Agent Framework variant as the modern replacement.

### Status (verified May 2026)

| Surface | C# / .NET | Python |
|---|---|---|
| Core (`Microsoft.Agents.AI`, `ChatClientAgent`, in-memory chat history) | **Released** | Public preview |
| `Microsoft.Agents.AI.CosmosNoSql` Cosmos chat history | Preview | Preview |
| RAG providers (Azure AI Search, Neo4j GraphRAG) | Mixed (TextSearchProvider released, Neo4j preview) | Preview |
| Memory providers (Mem0, Neo4j, Redis, Purview) | Mixed / preview | Preview |
| UI integrations (AG UI, Dev UI, Purview) | Preview | Preview |

Source: https://learn.microsoft.com/agent-framework/integrations/

### Key technical mapping (Semantic Kernel → Agent Framework, .NET)

| Concern | Semantic Kernel | Microsoft Agent Framework |
|---|---|---|
| Top-level using | `using Microsoft.SemanticKernel; using Microsoft.SemanticKernel.Agents;` | `using Microsoft.Extensions.AI; using Microsoft.Agents.AI;` |
| Base agent type | `SemanticKernel.Agents.Agent` (abstract) | `AIAgent` (abstract) |
| Concrete agent | `ChatCompletionAgent`, `OpenAIAssistantAgent`, `AzureAIAgent` (one per backend) | `ChatClientAgent` (one type — backend supplied via `IChatClient`) |
| DI: AI service | `services.AddKernel().AddProvider(...)` and require `Kernel` on the agent | `services.AddSingleton<IChatClient>(sp => new AzureOpenAIClient(...).GetChatClient(deployment).AsIChatClient());` |
| DI: agent | `services.AddKeyedSingleton<SemanticKernel.Agents.Agent>("name", ...)` with `Kernel = ...` | `services.AddKeyedSingleton<AIAgent>(() => client.AsAIAgent(...));` |
| Plugin / tool | Class with `[KernelFunction, Description("…")]` methods; `Kernel.Plugins.Add(KernelPluginFactory.CreateFromType<T>(serviceProvider))` | Method with `[Description("…")]` (optional); `chatOptions.Tools.Add(AIFunctionFactory.Create(MyTool.Method))` |
| Invocation | `await agent.InvokeAsync(input, thread)` returns `ChatMessageContent` | `await agent.RunAsync(input, session)` returns `AgentRunResponse` |
| Streaming | `agent.InvokeStreamingAsync(input, thread)` returns `StreamingChatMessageContent` | `agent.RunStreamingAsync(input, session)` returns `AgentResponseUpdate` |
| Options | `OpenAIPromptExecutionSettings { MaxTokens = 1000 }` wrapped in `AgentInvokeOptions { KernelArguments = ... }` | `ChatClientAgentRunOptions(new ChatOptions { MaxOutputTokens = 1000 })` |
| Agent construction | `new ChatCompletionAgent { Name = ..., Instructions = ..., Kernel = ... }` | `new ChatClientAgent(chatClient, new ChatClientAgentOptions { Name = ..., Instructions = ..., ChatOptions = toolOptions })` |

Source for code shapes: https://learn.microsoft.com/microsoft-365/agents-sdk/using-semantic-kernel-agent-framework + https://learn.microsoft.com/agent-framework/migration-guide/from-semantic-kernel/

### Wiring it into an Agents SDK app

The `Program.cs` and `AgentApplication` (or `ActivityHandler`) shape stays the same. The only change is what you register and what you call from the turn handler:

```csharp
// Program.cs — register the chat client and agent
builder.Services.AddSingleton<IChatClient>(sp =>
    new AzureOpenAIClient(endpointUri, apiKeyCredential)
        .GetChatClient(deployment)
        .AsIChatClient());

// Inside your Agents SDK agent (e.g., EchoBot.cs)
private readonly ChatClientAgent _afAgent;

public EchoBot(AgentApplicationOptions options, IChatClient chatClient) : base(options)
{
    var toolOptions = new ChatOptions
    {
        Temperature = 0.2f,
        Tools = new List<AITool>
        {
            AIFunctionFactory.Create(DateTimeFunctionTool.GetDate),
        },
    };

    _afAgent = new ChatClientAgent(
        chatClient,
        new ChatClientAgentOptions
        {
            Name = "Helper",
            Instructions = "You are a helpful assistant.",
            ChatOptions = toolOptions,
        });
}
```

The Agents SDK still owns `Activity` / `TurnContext` / state / channels. Agent Framework owns the LLM call, tool execution, streaming, and (optionally) workflow graph.

## The five orchestration options

| Option | What it is | Typical use case |
|---|---|---|
| **Microsoft Agent Framework** | Microsoft's open-source successor to Semantic Kernel + AutoGen. C# released core, Python preview. Unified `IChatClient` + `ChatClientAgent`. | **Default recommendation** for pro-code .NET / Python teams new to orchestration or starting fresh. |
| **Azure AI Foundry** | Hosted agent service + model catalog. Bring an existing Foundry agent into your Agents-SDK app. | You already have a Foundry agent / want one managed model + agent runtime in Azure. Often pairs with Agent Framework. |
| **Semantic Kernel** | The predecessor framework. Still supported and documented. Has an official migration guide to Agent Framework. | Existing SK codebase you can't migrate yet, or skill-set parity with an existing SK team. |
| **OpenAI Agents** | OpenAI's Assistants / Agents API. | Teams already standardized on OpenAI directly. |
| **LangChain** | Third-party open-source orchestration. | Teams already invested in LangChain (often Python). |
| **Custom / your own** | Direct LLM API calls + your own logic. | You want the framework out of the way. |

The Agents Toolkit ships templates (e.g., the Weather Agent) with Foundry pre-wired. Older templates may scaffold Semantic Kernel — for new work, follow the SK → Agent Framework migration guide or start with Agent Framework directly.

## When the existing bot is on LUIS / QnA — the replacement recipe

LUIS and QnA Maker are **retired services**. The Agents SDK migration page is explicit:

> LUIS and QnA Maker are retired, so replace any remaining usage with supported approaches such as Azure OpenAI with retrieval.

Recipe Microsoft recommends:

1. **Azure OpenAI** (chat completion / embeddings model) for natural language.
2. **Retrieval** for grounding — Azure AI Search, Microsoft Graph (Retrieval API), or your own vector store.
3. **Orchestration** — Foundry agents *or* Microsoft Agent Framework workflows *or* LangChain chains, depending on the team's preference.

That triple replaces:

| Old | New |
|---|---|
| LUIS intent classification | LLM with system prompt / function calling |
| LUIS entity extraction | LLM + structured outputs / function calling |
| QnA Maker FAQ | Azure AI Search + RAG, or knowledge sources in a Foundry agent |
| Orchestrator (legacy multi-model router) | Foundry orchestration or Microsoft Agent Framework workflows |

## When to pick which

Working rules of thumb based on the MS Learn scenarios:

| Signal | Suggested path |
|---|---|
| Pure pro-code .NET team, want Microsoft-owned stack end-to-end | **Microsoft Agent Framework** + Azure OpenAI / Foundry |
| Already operating Foundry agents | **Foundry agent integration** (portal or via Agents Toolkit proxy), orchestrated from Agent Framework |
| Multi-step agentic workflows, tools / function calling primary | **Microsoft Agent Framework** workflows or **OpenAI Agents** |
| Python team already on LangChain | **LangChain** + Azure OpenAI |
| RAG-heavy, lots of documents to ground on | Azure AI Search + Agent Framework / Foundry retrieval |
| Replacing LUIS specifically | LLM with structured outputs + small Agent Framework / Foundry orchestrator |
| Replacing QnA Maker specifically | Azure AI Search + RAG, or Foundry agent with knowledge sources |
| Existing Semantic Kernel code, can't migrate yet | Stay on **Semantic Kernel**, plan migration via the official SK → Agent Framework migration guide |

## Knowledge access (data the agent grounds on)

From the custom-engine-agent overview's "Knowledge source access" note:

- **Copilot Studio agents** have native access to Microsoft 365 + Copilot connectors.
- **Pro-code agents (Agents SDK / Foundry via Toolkit)** access the same data via:
  - Microsoft Graph APIs
  - The **Retrieval API** for grounding in Microsoft 365 data

For grounding scenarios on enterprise content: the SDK doesn't ship a knowledge layer — wire in Azure AI Search or whatever vector store the team already uses. Agent Framework ships RAG context providers (`TextSearchProvider`, Azure AI Search provider, Neo4j GraphRAG) you can drop onto a `ChatClientAgent`.

## Stretch goal for this repo (gated)

Per PRD § 4.5: a second `after-agents-sdk-with-foundry/` sample that drops Azure AI Foundry + **Microsoft Agent Framework** into the same scenario to show the LUIS/QnA replacement pattern in practice. Build only if the team's existing bot uses LUIS or QnA — confirm scope first.

(Earlier drafts of this doc called for "Foundry + Semantic Kernel" here. Updated to Agent Framework to track the current MS Learn guidance.)

## Out-of-scope for v1

- Multi-agent orchestration patterns (one agent calling another) — Agent Framework workflows cover this; separate doc.
- Agent-to-agent OAuth / federated identity — covered conceptually in the Entra Agent Identity Blueprints space but out of scope for this migration baseline.
